"""Generate four small ElevenLabs cues into ignored staging, ready for Editor import.

Uses ELEVENLABS_API_KEY or the device-local OpenClaw credential. Never copies credentials
to the project. Existing successful responses are reused; --regenerate requests new audio.
API reference: https://elevenlabs.io/docs/api-reference/text-to-sound-effects/convert
"""
import argparse
import array
import hashlib
import json
import math
import os
from pathlib import Path
import subprocess
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
STAGING = ROOT / 'Artifacts' / 'generated-audio'
MODEL = 'eleven_text_to_sound_v2'
STYLE = (' Single isolated cozy nature game sound. Soft rounded attack and smooth short decay, '
         'warm and understated, no harsh treble, no click, no crackle, no distortion, '
         'no voice, no speech, no music or background ambience.')
CUES = {
    'Spawn': ('A tiny friendly rounded water droplet blooming gently upward, a welcoming animal appearance cue.' + STYLE, 0.6, -18),
    'Hop': ('One soft cartoon frog hop, a small rubbery low boing with a light muted grass landing, very gentle and tactile.' + STYLE, 0.5, -22),
    'Eat': ('Two tiny soft cartoon munch sounds, shup shup, a cute creature eating a berry. Muted squishy movement, no saliva or crunch.' + STYLE, 0.7, -18),
    'Click': ('One quiet warm wooden marimba note, a gentle felt mallet plop for a game button, rounded and very short.' + STYLE, 0.5, -21),
}


def pcm(path, *filters):
    command = ['ffmpeg', '-v', 'error', '-i', str(path)]
    if filters:
        command += ['-af', ','.join(filters)]
    command += ['-ar', '44100', '-ac', '1', '-f', 'f32le', 'pipe:1']
    samples = array.array('f')
    samples.frombytes(subprocess.run(command, capture_output=True, check=True).stdout)
    return samples


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--regenerate', action='store_true')
    args = parser.parse_args()
    key = os.environ.get('ELEVENLABS_API_KEY')
    credential = Path.home() / '.openclaw/credentials/elevenlabs.json'
    if not key and credential.is_file():
        key = json.loads(credential.read_text()).get('api_key')
    if not key:
        raise SystemExit('Set ELEVENLABS_API_KEY or configure the local ElevenLabs credential.')
    STAGING.mkdir(parents=True, exist_ok=True)
    report = {'provider': 'ElevenLabs', 'model': MODEL, 'sampleRate': 44100,
              'mastering': '70 Hz high-pass, 4 kHz low-pass, 25 ms attack, 120 ms fade, per-cue peak trim', 'cues': {}}
    for name, (prompt, duration, target_db) in CUES.items():
        raw = STAGING / (name + '-raw.mp3')
        if args.regenerate or not raw.exists():
            body = json.dumps({'text': prompt, 'duration_seconds': duration,
                               'prompt_influence': 0.8, 'model_id': MODEL}).encode()
            request = urllib.request.Request('https://api.elevenlabs.io/v1/sound-generation', data=body,
                                             headers={'xi-api-key': key, 'Content-Type': 'application/json'})
            try:
                with urllib.request.urlopen(request, timeout=120) as response:
                    audio = response.read()
            except urllib.error.HTTPError as error:
                raise SystemExit(f'{name}: ElevenLabs HTTP {error.code}; no automatic billed retry.') from None
            raw.write_bytes(audio)
        filters = ['aformat=sample_rates=44100:channel_layouts=mono', f'atrim=duration={duration}', 'highpass=f=70', 'lowpass=f=4000',
                   'afade=t=in:d=0.025', f'afade=t=out:st={duration - 0.12}:d=0.12']
        samples = pcm(raw, *filters)
        peak = max(map(abs, samples), default=0)
        if peak < 1e-5:
            raise SystemExit(f'{name}: response is silent; not installing it.')
        gain = 10 ** (target_db / 20) / peak
        path = STAGING / (name + '.wav')
        subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(raw), '-af', ','.join(filters + [f'volume={gain}']),
                        '-ar', '44100', '-ac', '1', '-c:a', 'pcm_s16le', str(path)], check=True)
        final = pcm(path)
        peak = max(map(abs, final))
        rms = math.sqrt(sum(sample * sample for sample in final) / len(final))
        if peak >= 0.32 or rms < 1e-6:
            raise SystemExit(f'{name}: final audio did not pass level checks.')
        report['cues'][name] = {'prompt': prompt, 'durationSeconds': round(len(final) / 44100, 3),
                               'peakDbFS': round(20 * math.log10(peak), 2), 'rmsDbFS': round(20 * math.log10(rms), 2),
                               'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
        print(name, 'ready', report['cues'][name]['durationSeconds'], 'seconds', flush=True)
    (ROOT / 'Documentation' / 'Audio.json').write_text(json.dumps(report, indent=2) + '\n')
    print('All four cues decoded, mastered and staged. Import through ZooProjectBuilder.', flush=True)


if __name__ == '__main__':
    main()
