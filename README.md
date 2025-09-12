# SubtitleSplitter

SubtitleSplitter is a simple command-line tool written in C# that allows you to convert a plain text file into a series of subtitles in `.srt` format. It's designed to take a text file containing a script or transcript and break it into chunks that can be used as subtitles for video or audio content.

## Features

- Read a text file and split it into sentences.
- Convert sentences into a subtitle file format.
- Sensible defaults for timing and wrapping.
  - One sentence per caption
  - Duration = characters / 15 cps (average reading speed)
  - No gap between consecutive captions
  - Wrapped to max 2 lines at ~42 characters per line (no truncation)

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

Run the tool with a single argument: the path to your input `.txt` file. It will generate an `.srt` file in the same directory named `<input>_subtitles.srt` using default settings.

```bash
# Windows
SubtitleSplitter.exe "path/to/your/file.txt"

# Cross-platform
dotnet run --project src/SubtitleSplitter -- "path/to/your/file.txt"
```

Behavior is fixed to simple defaults: one sentence per subtitle, average reading speed timing, zero inter-caption gap, and up to 2 lines at ~42 characters per line.

## Contributing

If you would like to contribute to the development of SubtitleSplitter, please fork the repository and submit a pull request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
