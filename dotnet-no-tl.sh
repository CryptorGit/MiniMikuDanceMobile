#!/usr/bin/env bash
# dotnet wrapper that disables TerminalLogger for environments without full terminal support
export MSBUILDTERMINALLOGGER=false
dotnet "$@"
