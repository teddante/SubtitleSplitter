import re
import argparse

def read_text_file(file_path):
    with open(file_path, 'r', encoding='utf-8') as file:
        return file.read()

def split_into_sentences(text):
    sentences = re.split(r'(?<=[.!?]) +', text)
    return sentences

def pair_sentences(sentences, num_sentences_per_group):
    return [sentences[i:i + num_sentences_per_group] for i in range(0, len(sentences), num_sentences_per_group)]

def calculate_display_times(sentence_pairs, words_per_minute):
    display_times = []
    for pair in sentence_pairs:
        word_count = len(' '.join(pair).split())
        display_time_seconds = (word_count / words_per_minute) * 60
        display_times.append(display_time_seconds)
    return display_times

def format_timecode(seconds):
    hours, seconds = divmod(seconds, 3600)
    minutes, seconds = divmod(seconds, 60)
    milliseconds = int(seconds * 1000 % 1000)
    return f"{int(hours):02}:{int(minutes):02}:{int(seconds):02},{milliseconds:03}"

def format_subtitles(sentence_pairs, display_times):
    subtitles = []
    start_time = 0
    for index, (pair, duration) in enumerate(zip(sentence_pairs, display_times)):
        end_time = start_time + duration
        start_timecode = format_timecode(start_time)
        end_timecode = format_timecode(end_time)
        subtitle_text = ' '.join(pair).replace('\n', ' ')  # Replace newlines with spaces
        subtitles.append(f"{index+1}\n{start_timecode} --> {end_timecode}\n{subtitle_text}\n")
        start_time += duration  # Increment start_time for the next pair
    return subtitles

def save_subtitle_file(subtitles, output_file_path):
    with open(output_file_path, 'w', encoding='utf-8') as file:
        file.write('\n'.join(subtitles))

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Convert text to subtitle file with customizable sentence grouping.")
    parser.add_argument('file_path', help='Path to the input text file')
    parser.add_argument('output_file_path', help='Path to the output subtitle file')
    parser.add_argument('words_per_minute', type=int, help='Average reading speed in words per minute')
    parser.add_argument('--sentences_per_caption', type=int, default=2, help='Number of sentences per subtitle caption')

    args = parser.parse_args()

    text_content = read_text_file(args.file_path)
    sentences = split_into_sentences(text_content)
    sentence_pairs = pair_sentences(sentences, args.sentences_per_caption)
    display_times = calculate_display_times(sentence_pairs, args.words_per_minute)
    subtitles = format_subtitles(sentence_pairs, display_times)
    save_subtitle_file(subtitles, args.output_file_path)
