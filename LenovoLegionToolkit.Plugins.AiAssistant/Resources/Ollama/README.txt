Built-in Ollama Instructions
============================

To use the built-in Ollama feature, place the Ollama executable files in this directory.

Required files:
- ollama.exe (main executable)

Optional files (if needed by Ollama):
- Any DLL dependencies
- Configuration files

Directory structure:
Resources/Ollama/
  ├── ollama.exe
  └── (other required files)

The plugin will automatically detect and use the built-in Ollama if:
1. The "Use Built-in Ollama" option is enabled in settings
2. ollama.exe exists in the Ollama directory

Note: You can download Ollama from https://ollama.ai/
After installation, copy the ollama.exe file to this directory.

