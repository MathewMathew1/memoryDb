# In-Memory Redis-Compatible Server

This repository contains a simplified in-memory Redis-compatible server implemented in .NET, along with a Python client and test scripts.

## Structure

- `dotnet/` — Source code for the custom .NET server. Supports a subset of Redis protocol, including replication and RDB snapshot loading.
- `python/` — Python scripts for testing and interacting with the server.

## Features

- Partial Redis protocol support.
- In-memory key-value storage (strings, lists, steams).
- Basic RDB file format reading and writing.
- Replica synchronization via FULLRESYNC.
- Transaction.

## Usage

1. Build and run the server in the `dotnet/` directory.
2. Use the Python client in `python/` to send commands or replicate data.

## Requirements
