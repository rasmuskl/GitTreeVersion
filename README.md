# GitTreeVersion

[![NuGet](https://img.shields.io/nuget/v/GitTreeVersion)](https://www.nuget.org/packages/GitTreeVersion/)

## Context and Scope

Versioning and in particular releasing software that has or has not changed is difficult. Many projects exist in a space where multiple seperately versionable deployables exist in the same repository. This is common in monorepos but polyrepo boundaries are also not always drawn to isolate each and every deployable.

This project aims to target these problems presented by this setup by combining git based versioning with a conservative dependency walker.

In addition to this, different software benefits from different versioning schemes inclusikgn semantic versioning to calendar versioning. This flexibility should also be supported by this project. 

## Goals

- Multiple nested versionable deployables inside a single repository 
- Version of individual deployable only affected by changes to relevant files 
- Provide stable monotonic versions for each branch
- Rely on git plumbing over traversing file systems
- Fast even in big repositories
- Never move HEAD
- No reliance on tags
- No reliance on magic commits

## Concepts

- Version root
- Deployable

## Usage

The command line tool `gtv` can be installed as a dotnet tool using: 

```
dotnet tool update --global GitTreeVersion
``` 

### Azure Pipelines Example

Basic yaml task to applying versions to all deployables within the repository:

```
- script: |
    dotnet tool update --global GitTreeVersion
    gtv version --apply --set-build-number
  displayName: Apply GitTreeVersion
```
