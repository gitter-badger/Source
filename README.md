## Turbo

[![Join the chat at https://gitter.im/turbo/src](https://badges.gitter.im/turbo/src.svg)](https://gitter.im/turbo/src?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Turbo is an open-source programming language for .NET and Mono. Like C# and VB.NET it generates (MS)IL assemblies which can be used in other .NET projects. The syntax is ECMAScript 4 plus some necessary additions for working with .NET. 

Turbo:

- Is a complete rewrite of JScript.NET (minus some broken and legacy features nobody used anyway).
- Is a compiler and debugger contained in a single .NET assembly (Turbo.Runtime.dll).
- Features a VSA-like protocol (THP - Turbo Hosting Protocol), so other .NET languages can compile Turbo code JIT.
- Is supported and actively developed unlike JScript.NET
- Actually works and is 99% JScript.NET compatible unlike Mono's MJS [attempt](http://www.mono-project.com/archived/jscript/) (dropped '08).
- Is one project for Mono and .NET builds but keeps all Windows-exclusive functionality, too! E.g. P/Invokes, COM-Interfacing.

## Getting Started

Installing Turbo couldn't be easier, for Windows and OS X it's just point-and-click and for all supported linux distros, it's less than 30 characters of sh code to install Mono and compile and install Turbo.

**Installation instructions** are here: [turbolang.org](http://turbolang.org).  
**Documentation** is here: [docs.turbolang.org](http://docs.turbolang.org).

Do not install Turbo when the `master` build status below is pending or failing.

## Status & Branches

Branch   | Status 1 | Status 2 | Info
-------- | ----- | ------- | ----
[`master`](https://github.com/turbo/src)   | [![Build Status](https://travis-ci.org/turbo/src.svg?branch=master)](https://travis-ci.org/turbo/src) | [![Build status](https://ci.appveyor.com/api/projects/status/5lnscvql1phw36oh/branch/master?svg=true)](https://ci.appveyor.com/project/minxomat/src/branch/master) | current stable release
[`unstable`](https://github.com/turbo/src/tree/unstable) | [![Build Status](https://travis-ci.org/turbo/src.svg?branch=unstable)](https://travis-ci.org/turbo/src) | [![Build status](https://ci.appveyor.com/api/projects/status/5lnscvql1phw36oh/branch/unstable?svg=true)](https://ci.appveyor.com/project/minxomat/src/branch/unstable) | current active development
[`gh-pages`](https://github.com/turbo/src/tree/gh-pages) | / | / | the [turbolang.org](http://turbolang.org) website
[`docs`](https://github.com/turbo/src/tree/docs) | [![](https://img.shields.io/badge/gitbook-edit-blue.svg)](https://www.gitbook.com/book/turbo/mergedocs/edit#/edit/docs/README.md) | [![](https://img.shields.io/badge/gitbook-get%20pdf-blue.svg)](https://www.gitbook.com/download/pdf/book/turbo/mergedocs) | documentation at [docs.turbolang.org](http://docs.turbolang.org/)

## Contributing 

**Code**

- Fork this repo.
- Switch to the development branch `unstable`.
- Make your additions (only leave comments in the source when it's a `// TODO:` or `// BUG:`).
- Use sensible commit messages.
- Send a pull request.
- The CLA bot will have you sign the CLA (TL;DR: Your additions will be licensed under MIT) and you'll get added to this orga.
- The automated builds and tests from Travis CI (Mono) and AppVeyor (Windows .NET) will kick in.
- If one of the builds fail, fix your mistakes.
- I'll take a look and merge into `unstable`.

**Docs**

It is not possible to contribute to the documentation as of now.
