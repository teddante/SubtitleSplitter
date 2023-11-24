import unittest
from text_to_srt import (read_text_file, split_into_sentences, pair_sentences,
                                calculate_display_times, format_timecode, format_subtitles)

class TestTextToSRT(unittest.TestCase):

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

    def test_read_text_file(self):
        with open('test_file.txt', 'w') as f:
            f.write('This is a test file.')
        self.assertEqual(read_text_file('test_file.txt'), 'This is a test file.')
        with self.assertRaises(IOError):
            read_text_file('non_existent_file.txt')

    def test_split_into_sentences(self):
        text = 'This is sentence one. This is sentence two! Is this sentence three?'
        self.assertEqual(split_into_sentences(text), ['This is sentence one.', 'This is sentence two!', 'Is this sentence three?'])
        with self.assertRaises(ValueError):
            split_into_sentences(123)

    def test_format_timecode(self):
        self.assertEqual(format_timecode(3661.123), '01:01:01,123')
        with self.assertRaises(RuntimeError):
            format_timecode('invalid input')

    def test_format_subtitles(self):
        sentence_pairs = [['Sentence one.', 'Sentence two.'], ['Sentence three.', 'Sentence four.']]
        display_times = [1, 2]
        expected_output = ['1\n00:00:00,000 --> 00:00:01,000\nSentence one. Sentence two.\n', 
                           '2\n00:00:01,000 --> 00:00:03,000\nSentence three. Sentence four.\n']
        self.assertEqual(format_subtitles(sentence_pairs, display_times), expected_output)
        with self.assertRaises(RuntimeError):
            format_subtitles('invalid input', display_times)
            
if __name__ == '__main__':
    unittest.main()