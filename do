#!/bin/bash
case "$1" in
	"build")
		cd C7/ && msbuild -m
		;;
	"run")
		cd C7/ && Godot
		;;
	"test")
		cd EngineTests/ && dotnet test --logger "console;verbosity=detailed"
		;;
	*)
		echo "invalid \"do\" option \"$1\""
		;;
esac
