import re
import argparse
import os
import tempfile
import logging
from nltk.tokenize import sent_tokenize
import nltk

# Setup logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

# Check and download NLTK data if not present
try:
    nltk.data.find('tokenizers/punkt')
except LookupError:
    nltk.download('punkt')

def read_text_file(file_path):
    """Reads a text file and returns its content."""
    try:
        with open(file_path, 'r', encoding='utf-8') as file:
            return file.read()
    except FileNotFoundError as e:
        logging.error("The file was not found. Please check the path.")
        raise e
    except IOError as e:
        logging.error("Could not read the file. Please check if the file is accessible.")
        raise e

def split_into_sentences(text):
    """Splits text into sentences using NLTK for better accuracy."""
    return sent_tokenize(text)

def pair_sentences(sentences, num_sentences_per_group):
    """Groups sentences by the specified number per group."""
    if num_sentences_per_group < 1:
        raise ValueError("Number of sentences per caption must be at least 1.")
    return [sentences[i:i + num_sentences_per_group] for i in range(0, len(sentences), num_sentences_per_group)]

def calculate_display_times(sentence_pairs, words_per_minute):
    """Calculates the display time for each pair of sentences."""
    if words_per_minute <= 0:
        raise ValueError("Words per minute must be a positive integer.")
    return [(len(' '.join(pair).split()) / words_per_minute) * 60 for pair in sentence_pairs]

def format_timecode(seconds):
    """Converts seconds to an SRT timecode format."""
    hours, seconds = divmod(seconds, 3600)
    minutes, seconds = divmod(seconds, 60)
    milliseconds = int(seconds * 1000 % 1000)
    return f"{int(hours):02}:{int(minutes):02}:{int(seconds):02},{milliseconds:03}"

def format_subtitles(sentence_pairs, display_times):
    """Formats the sentence pairs into SRT format with display times."""
    subtitles = []
    start_time = 0
    for index, (pair, duration) in enumerate(zip(sentence_pairs, display_times)):
        end_time = start_time + duration
        start_timecode = format_timecode(start_time)
        end_timecode = format_timecode(end_time)
        subtitle_text = ' '.join(pair).replace('\n', ' ')
        subtitles.append(f"{index+1}\n{start_timecode} --> {end_timecode}\n{subtitle_text}\n")
        start_time = end_time  # Ensuring that next subtitle starts right after the previous one
    return subtitles

def save_subtitle_file(subtitles, output_file_path):
    """Saves the subtitles to a file, using a temporary file for atomic writes."""
    try:
        # Create a temporary file in the same directory as the output file
        dir_name = os.path.dirname(output_file_path)
        with tempfile.NamedTemporaryFile('w', delete=False, dir=dir_name, encoding='utf-8') as tmpfile:
            tmpfile.write('\n'.join(subtitles))
            tempname = tmpfile.name
        # Replace the target file with the temporary file
        os.replace(tempname, output_file_path)
    except IOError as e:
        logging.error("Could not write to the file. Please check if you have write permissions.")
        raise e

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

    try:
        text_content = read_text_file(args.file_path)
        sentences = split_into_sentences(text_content)
        sentence_pairs = pair_sentences(sentences, args.sentences_per_caption)
        display_times = calculate_display_times(sentence_pairs, args.words_per_minute)
        subtitles = format_subtitles(sentence_pairs, display_times)
        save_subtitle_file(subtitles, args.output_file_path)
        logging.info("Subtitle file created successfully at %s", args.output_file_path)
    except Exception as e:
        logging.error("An error occurred: %s", str(e))
        exit(1)
