import re

def read_text_file(file_path):
    # Read the text file and return the content.
    with open(file_path, 'r', encoding='utf-8') as file:
        return file.read()

def split_into_sentences(text):
    # Use regular expression to split text into sentences.
    sentences = re.split(r'(?<=[.!?]) +', text)
    return sentences

def pair_sentences(sentences):
    # Pair the sentences into groups of two.
    return [sentences[i:i + 2] for i in range(0, len(sentences), 2)]

def calculate_display_times(sentence_pairs, words_per_minute):
    # Calculate display times for each pair of sentences.
    display_times = []
    for pair in sentence_pairs:
        word_count = len(' '.join(pair).split())
        display_time_seconds = (word_count / words_per_minute) * 60
        display_times.append(display_time_seconds)
    return display_times

def format_subtitles(sentence_pairs, display_times):
    # Format the pairs into SRT format with display times.
    subtitles = []
    start_time = 0
    for index, (pair, duration) in enumerate(zip(sentence_pairs, display_times)):
        end_time = start_time + duration
        start_timecode = format_timecode(start_time)
        end_timecode = format_timecode(end_time)
        subtitle_text = ' '.join(pair)
        subtitles.append(f"{index+1}\n{start_timecode} --> {end_timecode}\n{subtitle_text}\n")
        start_time = end_time
    return subtitles

def format_timecode(seconds):
    # Convert seconds to SRT timecode format.
    hours, seconds = divmod(seconds, 3600)
    minutes, seconds = divmod(seconds, 60)
    return f"{int(hours):02}:{int(minutes):02}:{int(seconds):02},000"

def save_subtitle_file(subtitles, output_file_path):
    # Save the subtitles to a file.
    with open(output_file_path, 'w', encoding='utf-8') as file:
        file.write('\n'.join(subtitles))

# Usage example:
file_path = 'path/to/text/file.txt'
output_file_path = 'path/to/output/file.srt'
words_per_minute = 150  # The average reading speed in words per minute.

text_content = read_text_file(file_path)
sentences = split_into_sentences(text_content)
sentence_pairs = pair_sentences(sentences)
display_times = calculate_display_times(sentence_pairs, words_per_minute)
subtitles = format_subtitles(sentence_pairs, display_times)
save_subtitle_file(subtitles, output_file_path)
