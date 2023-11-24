import re
import argparse
from nltk.tokenize import sent_tokenize

def read_text_file(file_path):
    try:
        with open(file_path, 'r', encoding='utf-8') as file:
            return file.read()
    except FileNotFoundError:
        print("The file was not found. Please check the path.")
        exit(1)
    except IOError:
        print("Could not read the file. Please check if the file is accessible.")
        exit(1)

def split_into_sentences(text):
    # Using NLTK for better sentence tokenization
    return sent_tokenize(text)

def pair_sentences(sentences, num_sentences_per_group):
    if num_sentences_per_group < 1:
        raise ValueError("Number of sentences per caption must be at least 1.")
    return [sentences[i:i + num_sentences_per_group] for i in range(0, len(sentences), num_sentences_per_group)]

def calculate_display_times(sentence_pairs, words_per_minute):
    display_times = []
    for pair in sentence_pairs:
        word_count = len(' '.join(pair).split())
        # Guard against words_per_minute being zero or negative
        if words_per_minute <= 0:
            raise ValueError("Words per minute must be a positive integer.")
        display_time_seconds = max((word_count / words_per_minute) * 60, 1)  # Ensure at least 1 second of display
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
        # Ensure subtitle durations do not overlap
        if index < len(display_times) - 1:
            next_duration = display_times[index + 1]
            if start_time + duration + next_duration > end_time:
                end_time = start_time + duration - 0.001  # Subtract 1 ms to prevent overlap
        start_timecode = format_timecode(start_time)
        end_timecode = format_timecode(end_time)
        subtitle_text = ' '.join(pair).replace('\n', ' ')  # Replace newlines with spaces
        subtitles.append(f"{index+1}\n{start_timecode} --> {end_timecode}\n{subtitle_text}\n")
        start_time += duration  # Increment start_time for the next pair
    return subtitles

def save_subtitle_file(subtitles, output_file_path):
    try:
        with open(output_file_path, 'w', encoding='utf-8') as file:
            file.write('\n'.join(subtitles))
    except IOError:
        print("Could not write to the file. Please check if you have write permissions.")
        exit(1)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Convert text to subtitle file with customizable sentence grouping.")
    parser.add_argument('file_path', help='Path to the input text file')
    parser.add_argument('output_file_path', help='Path to the output subtitle file')
    parser.add_argument('words_per_minute', type=int, help='Average reading speed in words per minute')
    parser.add_argument('--sentences_per_caption', type=int, default=2, help='Number of sentences per subtitle caption')

    args = parser.parse_args()

    if args.words_per_minute <= 0:
        parser.error("Words per minute must be a positive integer.")
    if args.sentences_per_caption <= 0:
        parser.error("Number of sentences per caption must be a positive integer.")

    text_content = read_text_file(args.file_path)
    sentences = split_into_sentences(text_content)
    try:
        sentence_pairs = pair_sentences(sentences, args.sentences_per_caption)
    except ValueError as e:
        parser.error(str(e))
    display_times = calculate_display_times(sentence_pairs, args.words_per_minute)
    subtitles = format_subtitles(sentence_pairs, display_times)
    save_subtitle_file(subtitles, args.output_file_path)
