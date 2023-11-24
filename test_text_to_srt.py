import unittest
from subtitle_converter import (read_text_file, split_into_sentences, pair_sentences,
                                calculate_display_times, format_timecode, format_subtitles)

class TestSubtitleConverter(unittest.TestCase):

    def test_pair_sentences(self):
        sentences = ['Sentence one.', 'Sentence two.', 'Sentence three.', 'Sentence four.']
        self.assertEqual(pair_sentences(sentences, 2), [['Sentence one.', 'Sentence two.'], ['Sentence three.', 'Sentence four.']])
        self.assertEqual(pair_sentences(sentences, 1), [['Sentence one.'], ['Sentence two.'], ['Sentence three.'], ['Sentence four.']])
        self.assertEqual(pair_sentences(sentences, 3), [['Sentence one.', 'Sentence two.', 'Sentence three.'], ['Sentence four.']])
        with self.assertRaises(ValueError):
            pair_sentences(sentences, 0)

    def test_calculate_display_times(self):
        sentence_pairs = [['Sentence one.', 'Sentence two.'], ['Sentence three.', 'Sentence four.']]
        words_per_minute = 120
        expected_times = [(len('Sentence one. Sentence two.'.split()) / words_per_minute) * 60,
                          (len('Sentence three. Sentence four.'.split()) / words_per_minute) * 60]
        self.assertEqual(calculate_display_times(sentence_pairs, words_per_minute), expected_times)
        with self.assertRaises(ValueError):
            calculate_display_times(sentence_pairs, 0)

    # Add more tests for other functions like format_timecode, format_subtitles, etc.

if __name__ == '__main__':
    unittest.main()
