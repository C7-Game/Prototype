# GitHub Action scripts for C7

https://docs.github.com/en/actions/quickstart

## test-action.yml

I've been using test-action.yml to try out GitHub action features before integrating them into export-c7.yml, the build script. It's almost always in a state where it's safe to run, and it doesn't really do anything of note.

## export-c7.yml

This build action uses [the godot-ci:mono-\<version> Docker image](https://hub.docker.com/r/barichello/godot-ci) which is based on [the latest mono Docker image](https://hub.docker.com/_/mono) which together contain the mono SDK, headless Godot, and Godot export templates.

export-c7.yml has 3 jobs:

- Windows export

    The artifact from this job can be used as-is as a distributable C7

- Linux export

    There is a .tgz file inside the artifact, and the .tgz is the distributable Linux C7.
    The Linux script is different from the others in that it has a step to create
    a .tgz archive and then uploads that .tgz as an artifact.

- Mac export

    There is a .zip file inside the artifact, and the .zip is the distributable Mac C7.
    The default save json file is added to the distributable zip, but the Mac app can't
    reliably determine a path to it, so as of v0.0-aztec Mac is likely to crash unless
    a file is opened from the load game button. The Mac build script is a little different
    than the others in that the copy save task uses `zip` to place the file into the export.

## Things I've learned about actions

- To set an environment variable in a script, echo the setting into $GITHUB_ENV like `echo "MYVAR=my value" >> $GITHUB_ENV` https://docs.github.com/en/actions/learn-github-actions/workflow-commands-for-github-actions#environment-files
- After setting a variable as such, it is not available until the next step
- This seems to be because each step's run section is passed to a new shell instance at the same working directory entry point and set up with what is in $GITHUB_ENV
- In the env: section, you can't set environment variables based on other variables or in a conditional way based on the workflow inputs
- Inside run sections, environment variable references are the same as in the shell, e.g. `$MYVAR` or `${MYVAR}`, but as a YAML value reference need to be double-braced and prepended with "env." as such: `${{ env.MYVAR }}`
- You can make GitHub Actions do things by echoing commands starting with `::` in run scripts, e.g. `echo "::warning title=Box not checked::User did not click the box!"` will show a warning icon and message in the job output but not fail the job
- A manually run workflow is `workflow_dispatch`
- `workflow_dispatch` jobs can only run if they are in the repo default directory; this unfortunately makes for a lot of commit spam while testing and debugging, but the flow can run against any branch
- Other triggers can be in their own branches and work, like `on: [push]`

## A very brief tour of the GitHub Actions structure

``` yaml
name: The name of the action, visible in actions tab

# What triggers this action; this is an array and could also be expressed as
#   on: [workflow_dispatch] if there are no inputs or other modifiers
on:
  workflow_dispatch:
    # Inputs defined (optional)
    inputs:

env:
    MYVAR: This environment variable will be available in run scripts

# jobs run in parallel
jobs:
  # somewhat counter to step names, the job names are on the left of the colon
  Name-of-Job:
    # Different runtime platforms are available
    runs-on: ubuntu-latest
    # Optional; can run in a Docker container from Docker Hub
    container:
      image: barichello/godot-ci:mono-3.4
    steps:
    - name: Name of step
      # For single-line run steps, can just do `run: echo "doing a one-line thing"`
      run: |
        echo "This is running in a shell at the entry point working directory"
        echo "Each step starts with a new shell and environment from $GITHUB_ENV"
    # Not all steps need be run/scripts; in fact I think GitHub Actions
    #  is intended to mostly use predefined action steps like this
    #  This particular one clones the repo into the entry point working directory
    - name: Checkout
      uses: actions/checkout@v2
      with:
        lfs: true
```