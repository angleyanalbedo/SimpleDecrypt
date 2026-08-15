# SimpleCrypt

A small Windows GUI tool for encrypting and decrypting files and folders.

The project is intentionally focused on raw symmetric encryption. It does not add a custom file header, container format, or automatic algorithm detection.

## Supported algorithms

- AES-CBC
- DES-CBC
- TripleDES-CBC
- RC2-CBC

The same algorithm, Key, and IV must be selected when decrypting.

## Features

- English Windows Forms interface
- File encryption and decryption
- Recursive folder processing
- Drag and drop input
- Progress bar
- Optional overwrite of existing files
- Plain-text and hexadecimal Key/IV input
- Streaming file processing for large files

Folder processing is performed file by file. The directory structure is preserved; folders are not packed into a custom archive.

Encrypted files receive the `.enc` suffix by default. The suffix is only a naming convention and is not used to detect the algorithm.

## Key and IV input

Plain text is converted to UTF-8 bytes:

```text
my-secret-key
```

Hexadecimal values can use either `hex:` or `0x`. Spaces and hyphens are accepted:

```text
hex:00112233445566778899AABBCCDDEEFF
0x0011 2233 4455 6677 8899 AABB CCDD EEFF
```

Required sizes:

| Algorithm | Key | IV |
| --- | --- | --- |
| AES-CBC | 16, 24, or 32 bytes | 16 bytes |
| DES-CBC | 8 bytes | 8 bytes |
| TripleDES-CBC | 16 or 24 bytes | 8 bytes |
| RC2-CBC | 5 to 16 bytes | 8 bytes |

The tool uses CBC mode with PKCS7 padding.

## Requirements

- Windows
- .NET Framework 4.8

The application targets .NET Framework 4.8 so the published executable remains small and does not need to include a self-contained runtime.

## Build

Open a Developer PowerShell or Visual Studio environment and run:

```powershell
dotnet build SimpleDecrypt.csproj --configuration Release
```

The executable is generated at:

```text
bin\Release\net48\SimpleDecrypt.exe
```

## Security note

DES, TripleDES, and RC2 are legacy algorithms and should only be used when compatibility requires them. AES-CBC is provided for compatibility and interoperability; this version does not provide authenticated encryption or tamper detection.
