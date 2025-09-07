# SubtitleSplitter

SubtitleSplitter is a simple command-line tool written in C# that allows you to convert a plain text file into a series of subtitles in `.srt` format. It's designed to take a text file containing a script or transcript and break it into chunks that can be used as subtitles for video or audio content.

## Features

- Read a text file and split it into sentences.
- Convert sentences into a subtitle file format.
- Customize the number of sentences per subtitle.
- Customize the gap duration between subtitles.

## Getting Started

### Prerequisites

- .NET 8 SDK

### Installation

Clone the repository to your local machine using:

```bash
git clone https://github.com/yourusername/SubtitleSplitter.git
```

Navigate into the cloned repository:

```bash
cd SubtitleSplitter
```

Build the project using:

```bash
dotnet build
```

### Usage

To use SubtitleSplitter, simply drag a `.txt` file onto the executable or run it from the command line with the file path as an argument.

```bash
# Windows
SubtitleSplitter.exe "path/to/your/file.txt" [--sentences-per-subtitle 2] [--wpm 200] [--cps 15] [--gap 1.0] [--min-duration 1.0] [--max-duration 7.0] [--split-on-newline] [--output path|dir] [--max-line-length 42] [--max-lines 2]

# Cross-platform
dotnet run --project src/SubtitleSplitter -- "path/to/your/file.txt" [options]
```

The tool generates an `.srt` file in the same directory as the input file by default, or to the path specified by `--output` (file or directory). Use `--help` to see all options.

## Contributing

If you would like to contribute to the development of SubtitleSplitter, please fork the repository and submit a pull request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
