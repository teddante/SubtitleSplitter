import re

def read_text_file(file_path):
    try:
        with open(file_path, 'r', encoding='utf-8') as file:
            return file.read()
    except Exception as e:
        raise IOError(f"An error occurred while reading the file: {e}")

def split_into_sentences(text):
    try:
        sentences = re.split(r'(?<=[.!?]) +', text)
        return sentences
    except Exception as e:
        raise ValueError(f"An error occurred while splitting the text into sentences: {e}")

def pair_sentences(sentences, num_sentences_per_group):
    if num_sentences_per_group < 1:
        raise ValueError("Number of sentences per caption must be at least 1.")
    return [sentences[i:i + num_sentences_per_group] for i in range(0, len(sentences), num_sentences_per_group)]

def calculate_display_times(sentence_pairs, words_per_minute):
    if words_per_minute <= 0:
        raise ValueError("Words per minute must be a positive number.")
    display_times = []
    for pair in sentence_pairs:
        word_count = len(' '.join(pair).split())
        display_time_seconds = (word_count / words_per_minute) * 60
        display_times.append(display_time_seconds)
    return display_times

def format_timecode(seconds):
    try:
        hours, seconds = divmod(seconds, 3600)
        minutes, seconds = divmod(seconds, 60)
        milliseconds = int(seconds * 1000 % 1000)
        return f"{int(hours):02}:{int(minutes):02}:{int(seconds):02},{milliseconds:03}"
    except Exception as e:
        raise RuntimeError(f"An error occurred while formatting the timecode: {e}")

def format_subtitles(sentence_pairs, display_times):
    try:
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
    except Exception as e:
        raise RuntimeError(f"An error occurred while formatting the subtitles: {e}")

def save_subtitle_file(subtitles, output_file_path):
    try:
        with open(output_file_path, 'w', encoding='utf-8') as file:
            file.write('\n'.join(subtitles))
    except Exception as e:
        raise IOError(f"An error occurred while saving the file: {e}")
