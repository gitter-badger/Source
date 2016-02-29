# Turbo

Turbo is on open-source programming language for the .NET and Mono frameworks. It is fully compatible with both of them and has some extra features when running in Windows (like P/Invokes and COM-interfacing). This language is almost 100% compatible with the 2010 version of JScript 10.

The syntax is OOP-flavored JavaScript. Unlike other OOP languages, you don't have to use the actual OOP features at all. In fact, you can use Turbo like a scripting language even though it is a compiled language. The Turbo runtime implements the Turbo compiler and all of ECMAScript 3 and Draft-4, as well as more advanced OOP features and some .NET specific ones.

# Features

> For a demo of almost all features, see the `Turbo Binaries\test.js` file.

**Familiar Syntax**

The Turbo runtime implements the JavaScript (ES3 and Draft-ES4) syntax and adds object-oriented features such as Classes, Interfaces, Namespaces (packages) and Enumerators. Semicolons are optional.

```js
function printFacts(name, ... info : Object[])
{
	var s = "Name: " + name;
	s += "Info: " + info.length;
	for (var factNum in info) s += factNum + ": " + info[factNum];
	return s;
}
```

---

**Compatible**

Turbo runs on everything that can run Mono or .NET. Turbo assemblies can be used in other .NET projects (eg. C#) and other assemblies can be used in Turbo, without loosing any features.

```js
import System;
import System.Drawing;
import System.Windows.Forms;
import Accessibility;

main();

function main() {
	var form = new Form;
	...
	Application.Run(form);
}
```

---

**True JIT features**

Turbo assemblies carry a bootstrapped compiler which enables scripting-like JIT compilation of methods and true, JS-like, evaluation of expressions.

```js
function testGlobal_eval()
{
	var dateFn = "Date(1971,3,8)";
	var mydate;
	eval("mydate = new "+dateFn+";");
	return mydate.constructor == Date;
}
```

---

**Optional Typing & Advanced OOP**

Even though Turbo is a compiled language, you never have to specify data types (though you can to speed up your code). Use `var` just like in JS and let the compiler do the work.

```js
abstract class CAnimal
{
	abstract function printQualities() : String;
}

class CDog extends CAnimal
{
	function printQualities()
	{
		return "A dog has four legs.";
	}
}

class CKangaroo extends CAnimal
{
	function printQualities()
	{
		return "A kangaroo has a pouch.";
	}
}

var animalDog = new CDog;
var animalKangaroo = new CKangaroo;
```

---

**Safe Code**

Use the well-known `try...catch` error handling model and define your own error types. The compiler also warns you about code that is likely to fail and produces fully compliant `*.pdb` files. 

```js
function testError_Object()
{
	try
	{
		throw new Error(42, "No question");
	}
	catch(e)
	{
		return e + ", " + (e.number & 0xFFFF) + e.description;
	}
}
```

**Advanced Preprocessor**

Unlike other .NET language, Turbo actually has a PP which is able to calculate compile-time constants, not just define them.

```js
@set @myvar1 = (6 * 2)
@if (@myvar1 == 12)
	print("> PP works.");
@else
	print("> PP error.");
@end
```

# Building Turbo

> Only do this, if you absolutely have to. We recommend to download the binaries [from the release page](https://github.com/turbo/Source/releases).

Turbo has to be build on Windows. It is compatible with MSBuild (VS14), but not Mono's XBuild. The resulting assemblies will run in Mono, though. So if you want to use Turbo on linux, OS X or even other architectures (we only tested ARMv7h), just download the binaries from the github release page of this repository.

1. Clone or download this repository
2. Open `Turbo.sln`
3. Rebuild All

That's it. The only two files you need are `turbo.exe` and `Turbo.Runtime.dll` from the `.\Turbo Binaries\` directory. You can validate your build by opening a command prompt in the binary's directory and running

```
runtests
```

which should output this:

```
# Today's date is: 2/29/2016
# Today is: Monday
# Today's date is: 2/29/2016
# The current local time is: 5:52:0.530
# It's been 16860 days since 1/1/70
# The current local time is -1 hours before UTC
# Today is Monday in UTC.
# Today's UTC date is: 2/29/2016
# Current Coordinated Universal Time (UTC) is: 4:52:0.571
# Current setting is 29 September, 2016 05:52:00
# Current setting is 1 March, 2042 05:52:00
# Current setting is 29 February, 2016 01:23:45
# Current setting is 29 February, 2016 05:52:00.456
# Current setting is 29 February, 2016 05:23:45
# Current setting is 29 June, 2016 05:52:00
# Current setting is 29 February, 2016 05:52:23.456
# Current setting is Wed, 8 Mar 2124 06:44:27 UTC
# Current setting is Thu, 29 Sep 2016 04:52:00 UTC
# Current setting is Thu, 1 Mar 2345 04:52:00 UTC
# Current setting is Mon, 29 Feb 2016 01:23:45 UTC
# Current setting is Mon, 29 Feb 2016 04:52:00 UTC.456
# Current setting is Mon, 29 Feb 2016 04:23:45 UTC
# Current setting is Sun, 29 May 2016 04:52:00 UTC
# Current UTC milliseconds setting is 678
# 120234.79721445602
# 1.7976931348623157e+308
# 4.94065645841247e-324

C -
D - [Drive not ready]
E - VMware Tools

> PP works.
> Tests passed: 90/90
> Tests failed: 0/90
> Total time:   936 ms
```

# Getting Started

***tba***
