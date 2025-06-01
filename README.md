# BPtoPNDataCompiler

A utility for compiling BP data into PN format. This tool is developed with .NET Core and designed to run cross-platform.

## 🖥️ Requirements

- macOS 10.15 or later
- Windows 10 or later
- The Instructions for installing DotNet can be found [here](https://learn.microsoft.com/en-us/dotnet/core/install/macos#install-net)
- Will need to download DotNet from [here](https://dotnet.microsoft.com/en-us/download/dotnet)
- A terminal application (e.g., Terminal, iTerm2)

---

## 📥 Installing .NET SDK on macOS

1. Visit the [.NET Download page]([https://dotnet.microsoft.com/en-us/download/dotnet/6.0](https://dotnet.microsoft.com/en-us/download/dotnet)).
2. Scroll down to **macOS** and download the **.NET SDK (x64)** or **ARM64** version depending on your Mac architecture.
   - Intel Macs: use x64.
   - Apple Silicon (M1/M2/M3): use ARM64.
3. Run the downloaded installer and follow the prompts.
4. Once installed, verify the installation by running:

   ```bash
   dotnet --version
   ````

You should see the installed version number (e.g., `6.0.4`).

---

## 🔧 Cloning the Repository

Open your terminal and run:

```bash
git clone https://github.com/halosm1th/BPtoPNDataCompiler.git
cd BPtoPNDataCompiler
```

---

## ⚙️ Building the Project

Once inside the project directory:

```bash
dotnet build
```

This will restore dependencies and compile the project.

---

## 🚀 Running the Project

Run the compiled app using:

```bash
dotnet run
```

You can pass any required arguments as needed. If the program expects input files or specific parameters, be sure to include them in your command line call (you may need to consult the code or usage examples for details).

---

## 📁 Project Structure

```text
BPtoPNDataCompiler/
├── Program.cs          # Main entry point
├── ...                 # Other C# source files
├── BPtoPNDataCompiler.csproj
└── README.md           # Project documentation
```

---

## ❓ Troubleshooting

* **`dotnet: command not found`**
  Ensure the .NET SDK is correctly installed and added to your `$PATH`.

* **Project not building?**
  Make sure you're using the right version of the .NET SDK. Check the `.csproj` file for the target framework (e.g., `net6.0`) and install the corresponding SDK version.

---

## 🤝 Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss your ideas.

---

## 👤 Author

* [halosm1th](https://github.com/halosm1th)
