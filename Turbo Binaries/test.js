/*
 * Turbo Compiler Test Suite
 *
 * Note that this doesn't test any project-management options at all.
 *
 * Latest version to work in: 1.1.1.3
 *
 */

/* Unit 1: Comments */

// test


/* Unit 3: Non-reference features */

function testAnonymousFunction()
{
	var test = function(a, b)
	{
		return a + b;
	}

	return test(1, 2);
}

/* Unit 4: Reference features */

// Array Handling

function testArray()
{
	var my_array = new Array();
	for (var i = 0; i < 10; i++) my_array[i] = i;
	return my_array[4];
}

function testArray_concat()
{
	var a, b, c;
	a = new Array(1, 2, 3);
	b = "One";
	c = new Array(42, "Two");
	return a.concat(b, c);
}

function testArray_pop_push()
{
	var my_array = new Array();

	my_array.push(5, 6, 7);
	my_array.push(8, 9);

	var s = "";
	var number;

	number = my_array.pop();
	while (number != undefined)
    {
		s += number + " ";
		number = my_array.pop();
    }

	return s;
}

function testArray_join()
{
	var a = new Array(0, 1, 2, 3, 4);
	return a.join("-");
}

function testArray_length()
{
	var my_array = new Array();
	my_array[2] = "Test";
	my_array[6] = "Another Test";
	return my_array.length;
}

function testArray_reverse()
{
	var a = new Array(0, 1, 2, 3, 4);
	return a.reverse();
}

function testArray_slice()
{
	var myArray = new Array(4,3,5,65);
	return myArray.slice(0, -1);
}

function testArray_sort()
{
	var a = new Array("X" ,"y" ,"d", "Z", "v","m","r");
	return a.sort();
}

function testArray_shift()
{
	var ar = new Array(10, 11, 12);
	var s = "";

	while (ar.length > 0)
	{
		var i = ar.shift();
		s += i.toString() + " ";
	}

	return s;
}


function testConstants_undefined()
{
	var declared;
	if (declared == undefined) return 42;
}

function testControlflow_break()
{
	var i = 100;
	while (i --> 0)
	{
		if (i == 42) break;
	}
	return ++i;
}

function testControlflow_breakLabeled()
{
	var i = 0;
	Outer:
	for (;;)
	{
		while (i < 100)
		{
			if (i == 42) break Outer;
			i++;
		}
	}
	return i;
}

function testControlflow_continue()
{
	var s = "", i = 0;
	while (i < 10)
	{
		i++;
		if (i==5) continue;
		s += i;
	}
	return s;
}

function testControlflow_do_while()
{
	var i = 0;
	do i++; while (i < 10);
	return i;
}

function testControlflow_for()
{
	var myarray = new Array();
	for (var i = 0; i < 10; i++)
	{
		myarray[i] = i;
	}
	return myarray;
}

function testControlflow_for_in()
{
	var ret = "";
	var obj =
	{
		"a" : "Athens" ,
		"b" : "Belgrade",
		"c" : "Cairo"
	};

	for (var key in obj) ret += key + ":" + obj[key] + ", ";
	return ret;
}

function testControlflow_if_else()
{
	var x = 5, y = 7;
	if (x == 5)
		if (y == 6)
			return 17;
		else
			return 20;
}

function testControlflow_switch()
{
	var obj = new Date();
	switch (obj.constructor)
	{
		case Date:
			return "Object is a Date.";
			break;
		case Number:
			return "Object is a Number.";
			break;
		case String:
			return "Object is a String.";
			break;
		default:
			return "Object is unknown.";
	}
}

function testDate_getDate()
{
	var s = "Today's date is: ";
	var d = new Date();
	s += (d.getMonth() + 1) + "/";
	s += d.getDate() + "/";
	s += d.getYear();
	return s;
}

function testDate_getDay()
{
	var x = new Array("Sunday", "Monday", "Tuesday");
	x = x.concat("Wednesday","Thursday", "Friday");
	x = x.concat("Saturday");
	var d = new Date();
	var day = d.getDay();
   return "Today is: " + x[day];
}

function testDate_getFullYear()
{
	var d = new Date();
	var s = (d.getMonth() + 1) + "/";
	s += d.getDate() + "/";
	s += d.getFullYear();
	return "Today's date is: " + s;
}

function testDate_getHours()
{
	var s = "The current local time is: ";
	var d = new Date();
	s += d.getHours() + ":";
	s += d.getMinutes() + ":";
	s += d.getSeconds() + ".";
	s += d.getMilliseconds();
	return s;
}

function testDate_getTime()
{
	var MinMilli = 1000 * 60;
	var HrMilli = MinMilli * 60;
	var DyMilli = HrMilli * 24;
	var d = new Date();
	var t = d.getTime();
	var s = "It's been "
	return s + Math.round(t / DyMilli) + " days since 1/1/70";
}

function testDate_getTimezoneOffset()
{
	var s = "The current local time is ";
	var d = new Date();
	var tz = d.getTimezoneOffset();
	if (tz < 0)
		s += tz / 60 + " hours before UTC";
	else if (tz == 0)
		s += "UTC";
	else
		s += tz / 60 + " hours after UTC";
	return s;
}

function testDate_getUTCDay()
{
	var x = new Array("Sunday", "Monday", "Tuesday");
	x = x.concat("Wednesday","Thursday", "Friday");
	x = x.concat("Saturday");
	var d = new Date();
	var day = d.getUTCDay();
	return "Today is " + x[day] + " in UTC.";
}

function testDate_getUTCDate()
{
	var s = "Today's UTC date is: ";
	var d = new Date();
	s += (d.getUTCMonth() + 1) + "/";
	s += d.getUTCDate() + "/";
	s += d.getUTCFullYear();
	return s;
}

function testDate_getUTCHours()
{
	var d = new Date();
	var s = d.getUTCHours() + ":";
	s += d.getUTCMinutes() + ":";
	s += d.getUTCSeconds() + ".";
	s += d.getUTCMilliseconds();
	return "Current Coordinated Universal Time (UTC) is: " + s;
}

function testDate_setDate()
{
	var d = new Date();
	d.setDate(242);
	return "Current setting is " + d.toLocaleString();
}

function testDate_setFullYear()
{
	var d = new Date();
	d.setFullYear(2042);
	return "Current setting is " + d.toLocaleString();
}

function testDate_setHours()
{
	var d = new Date();
	d.setHours(01, 23, 45);
	return "Current setting is " + d.toLocaleString();
}

function testDate_setMilliseconds()
{
	var d = new Date();
	d.setMilliseconds(456);
	return "Current setting is " + d.toLocaleString() + "." + d.getMilliseconds();
}

function testDate_setMinutes()
{
	var d = new Date();
	d.setMinutes(23, 45);
	return "Current setting is " + d.toLocaleString();
}

function testDate_setMonth()
{
	var d = new Date();
	d.setMonth(5);
	return "Current setting is " + d.toLocaleString();
}

function testDate_setSeconds()
{
	var d = new Date();
	d.setSeconds(23, 456);
	return "Current setting is " + d.toLocaleString() + "." + d.getMilliseconds();
}

function testDate_setTime()
{
	var d = new Date();
	d.setTime(4865553867925);
	return "Current setting is " + d.toUTCString();
}

function testDate_setUTCDate()
{
	var d = new Date();
	d.setUTCDate(242);
	return "Current setting is " + d.toUTCString();
}

function testDate_setUTCFullYear()
{
	var d = new Date();
	d.setUTCFullYear(2345);
	return "Current setting is " + d.toUTCString();
}

function testDate_setUTCHours()
{
	var d = new Date();
	d.setUTCHours(01, 23, 45);
	return "Current setting is " + d.toUTCString();
}

function testDate_setUTCMilliseconds()
{
	var d = new Date();
	d.setUTCMilliseconds(456);
	return "Current setting is " + d.toUTCString() + "." + d.getUTCMilliseconds();
}

function testDate_setUTCMinutes()
{
	var d = new Date();
	d.setUTCMinutes(23, 45);
	return "Current setting is " + d.toUTCString();
}

function testDate_setUTCMonth()
{
	var d = new Date();
	d.setUTCMonth(4);
	return "Current setting is " + d.toUTCString();
}

function testDate_setUTCSeconds()
{
	var d = new Date();
	d.setUTCSeconds(45, 678);
	return "Current UTC milliseconds setting is " + d.getUTCMilliseconds();
}

function testDate_UTC()
{
	var MinMilli = 1000 * 60
	var HrMilli = MinMilli * 60
	var DyMilli = HrMilli * 24

	var t1 = Date.UTC(2345, 5 - 1, 10)
	var d = new Date();
	var t2 = d.getTime();
	var t3;

	if (t2 >= t1)
		t3 = t2 - t1;
	else
		t3 = t1 - t2;

	return t3 / DyMilli;
}

/* Function Defs START*/
interface IForm
{
	function blank() : String;
}

class CForm implements IForm
{
	function blank()
	{
		return "This is blank.";
	}
}

function addSquares(x, y)
{
	return x*x + y*y;
}

var zSq = addSquares(3.,4.);
var derivedForm = new CForm;
var baseForm = derivedForm;

function printFacts(name, ... info : Object[])
{
	var s = "Name: " + name;
	s += "Info: " + info.length;
	for (var factNum in info) s += factNum + ": " + info[factNum];
	return s;
}

class testDeclarations_this
{
	var color, make, model;

	function Car(color, make, model)
	{
		this.color = color;
		this.make = make;
		this.model = model;
	}
}

var testDeclarations_this_Instance = new testDeclarations_this;
testDeclarations_this_Instance.Car("Blue", "VW", "Polo");

function testDeclarations_with()
{
	var x, y;
	with (Math)
	{
		x = round((cos(3 * PI) + sin(LN10)) * 100);
		y = round((tan(14 * E)) * 100);
	}
	return [x, y];
}
/* Function Defs END */

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

function testJIT_Function()
{
	var add = new Function("x", "y", "return(x+y)");
	return add(2, 3);
}

function testJIT_length(a, b)
{
	return "Args expected: " + testJIT_length.length;
}

function testGlobal_eval()
{
	var dateFn = "Date(1971,3,8)";
	var mydate;
	eval("mydate = new "+dateFn+";");
	return mydate.constructor == Date;
}

function testGlobal_parseInt()
{
	return parseInt("12abc");
}

function testGlobal_parseFloat()
{
	return parseFloat("1.2abc");
}

function testMath_all()
{
	var x = 0.0;
	with (Math)
	{
		x += abs(0.75);
		x += acos(0.75);
		x += asin(0.75);
		x += atan(0.75);
		x += atan2(0.75, 0.75);
		x += ceil(0.75);
		x += cos(0.75);
		x += exp(0.75);
		x += floor(0.75);
		x += log(0.75);
		x += max(2, 4, 5, 2);
		x += min(2, 4, 5, 2);
		x += pow(12, 11);
		x += random();
		x += sin(0.75);
		x += sqrt(123);
		x += tan(0.75);
		x += E;
		x += LN2;
		x += LN10;
		x += LOG2E;
		x += LOG10E;
		x += PI;
		x += SQRT1_2;
		x += SQRT2;
	}
	return Math.floor(x);
}

function testObject_constructor()
{
	var x = new String("Hi");
	return (x.constructor == String) ? "Object is a String." : "Dunno";
}

function testObject_instanceof()
{
	var x = new Array();
	return x instanceof Array;
}

function testOperators_delete()
{
	var cities =
	{
		"a" : "Athens",
		"b" : "Belgrade",
		"c" : "Cairo"
	};

	delete cities.b;

	var s = "";
	for (var k in cities) s += cities[k];
	return s;
}

function testOperators_void()
{
	var mute = function()
	{
		return 42;
	};

	return void mute();
}

function testOperators_typeof()
{
	var x = "Hello";
	return typeof x;
}

function testRegExp_index()
{
	var src = "The rain in Spain.";
	var re = /\w+/g;
	var arr : Array;
	var s = "";
	while ((arr = re.exec(src)) != null) s += arr.index + "-" + arr.lastIndex + "," + arr;
	return s;
}

function testRegExp_source()
{
	var src = "Spain";
	var re = /in/g;
	var s1;

	if (re.test(src))
		s1 = " contains ";
	else
		s1 = " does not contain ";

	return "The string " + src + s1 + re.source + ".";
}

function testString_charAt()
{
	var str = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	return str.charAt(5);
}

function testString_charCodeAt()
{
	var str = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	return str.charCodeAt(5);
}

function testString_fromCharCode()
{
	return String.fromCharCode(112, 108, 97, 105, 110);
}

function testString_indexOf()
{
	var str1 = "BABEBIBOBUBABEBIBOBU"
	return str1.indexOf("BIBO", 0);
}

function testString_lastIndexOf()
{
	var str1 = "BABEBIBOBUBABEBIBOBU"
	return str1.lastIndexOf("B");
}

function testString_split()
{
	var s = "The rain in Spain.";
	return s.split(" ");
}

function testString_toLowerCase()
{
	var s = "I'M VERY LOUD!";
	return s.toLowerCase();
}

function testString_toUpperCase()
{
	var s = "i'm very quiet.";
	return s.toUpperCase();
}

function testString_length()
{
	var s = "Lorem ipsum dolor sit amet.";
	return s.length;
}

function testString_slice()
{
	var str = "hello world";
	return str.slice(0, 5);
}

function testString_match()
{
	var s = "Hello, World!";
	var r = /\w+/g;
	return s.match(r);
}

function testString_replace()
{
	var ss = "The rain in Spain.";
	var re = /(\S+)(\s+)(\S+)/g;
	return ss.replace(re, "$3$2$1");
}

function testString_search()
{
	var s = "The rain in Spain falls mainly in the plain.";
	var re = /falls/i;
	return s.search(re);
}

/* Class Tests */

class CPerson
{
	var name : String;
	var address : String;

	function CPerson(name)
	{
		this.name = name;
	}

	function printMailingLabel()
	{
		return name + ", " + address;
	}

	static function printBlankLabel()
	{
		return "-blank-";
	}
}

class CCustomer extends CPerson
{
	var billingAddress : String;
	var lastOrder : String;

	function CCustomer(name, creditLimit)
	{
		super(name);
		this.creditLimit = creditLimit;
	}

	private var creditLimit : double;

	function get CreditLimit()
	{
		return creditLimit;
	}
}

var John = new CPerson("John Doe");
John.address = "Some place remote.";

var Jane = new CCustomer("Jane Doe", 500.);
Jane.billingAddress = Jane.address = "Nice loft in NYC.";
Jane.lastOrder = "Windows 2008 Server";

/* Interface Tests */

interface IFormA
{
	function displayName() : String;
}

interface IFormB
{
	function displayName() : String;
}

class CFormITF implements IFormA, IFormB
{
	function displayName()
	{
		return "This the form name.";
	}
}

var iftestC = new CFormITF();
var iftestA : IFormA = iftestC;
var iftestB : IFormB = iftestC;

/* Properties */

class CCitizen
{
	private var privateAge : int;
	private var privateFavoriteColor : String;

	function CCitizen(inputFavoriteColor)
	{
		privateAge = 0;
		privateFavoriteColor = inputFavoriteColor;
	}

	function get Age()
	{
		return privateAge;
	}

	function set Age(inputAge)
	{
		privateAge = inputAge;
	}

	public final function get FavoriteColor()
	{
		SomeCode();
		return privateFavoriteColor;
	}

	private static function SomeCode()
	{
		return "I'm never visible";
	}

	protected function foobar1()
	{
		return 0;
	}

	internal function foobar2()
	{
		return 1;
	}
}

var chris = new CCitizen("red");
chris.Age = 27;

/* Abstract */

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

/* Overriding & Hiding */

class CBase
{
	function methodA()
	{
		return "methodA of CBase.";
	}

	function methodB()
	{
		return "methodB of CBase.";
	}
}

class CDerived extends CBase
{
	hide function methodA()
	{
		return "Hiding methodA.";
	}

	override function methodB()
	{
		return "Overriding methodB.";
	}
}

var hideTestBase = new CBase;
var hideTestDerv = new CDerived;

/* Constants */

class CSimple
{
	static public const constantValue = 42;
}

var constTest       = new CSimple;
const constIndex    = 5;
const constName     = "Thomas Jefferson";
const constAnswer   = 42;
const constOneThird = 1./3.;
const constThings = new Object[50];
constThings[1] = "thing1";

/* Enums */

enum CarType
{
	Honda,
	Toyota,
	Nissan = 42
}

var testCar : CarType = CarType.Nissan;

/* Dynamic classes and methods */

dynamic class CExpandoExample
{
	var x = 10;
}

var testClass  = new CExpandoExample;
var testObject = new Object;

testClass["x"]  = "ten";
testObject["x"] = "twelve";

class CExpandoExample2
{
	var x : int;

	dynamic function constructor(val)
	{
		this.x = val;
		return "Method called as a function.";
	}
}

var exptest = new CExpandoExample2;
var expstr = exptest.constructor(123);
var expobj = new exptest.constructor(456);

/* Enum objects */

function testEnumerator()
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	var e = new Enumerator(fso.Drives);
	var s = "", x, n;

	for (; !e.atEnd(); e.moveNext())
	{
		x = e.item();
		s += x.DriveLetter + " - ";

		if (x.DriveType == 3)
			n = x.ShareName;
		else if (x.IsReady)
			n = x.VolumeName;
		else
			n = "[Drive not ready]";

		s += n + "\n";
	}

	return s;
}

/* Namespaces */

package Deutschland
{
	class Greeting
	{
		static var Hello = "Guten tag!";
	}
}

package France
{
	public class Greeting
	{
		static var Hello = "Bonjour!";
	}

	public class Units
	{
		static var distance = "meter";
	}
}

package France.Paris
{
	public class Landmark
	{
		static var Tower = "Eiffel Tower";
	}
}

class Greeting
{
	static var Hello = "Greetings!";
}

import Deutschland;
import France;
import France.Paris;

import System.Diagnostics;

var unitTimer = new Stopwatch();
unitTimer.Start();

var passedTests = 0;
var failedTests = 0;
var numberTests = 0;

/* Perform tests */
print("# " + testDate_getDate());
print("# " + testDate_getDay());
print("# " + testDate_getFullYear());
print("# " + testDate_getHours());
print("# " + testDate_getTime());
print("# " + testDate_getTimezoneOffset());
print("# " + testDate_getUTCDay());
print("# " + testDate_getUTCDate());
print("# " + testDate_getUTCHours());
print("# " + testDate_setDate());
print("# " + testDate_setFullYear());
print("# " + testDate_setHours());
print("# " + testDate_setMilliseconds());
print("# " + testDate_setMinutes());
print("# " + testDate_setMonth());
print("# " + testDate_setSeconds());
print("# " + testDate_setTime());
print("# " + testDate_setUTCDate());
print("# " + testDate_setUTCFullYear());
print("# " + testDate_setUTCHours());
print("# " + testDate_setUTCMilliseconds());
print("# " + testDate_setUTCMinutes());
print("# " + testDate_setUTCMonth());
print("# " + testDate_setUTCSeconds());
print("# " + testDate_UTC());

proof(testAnonymousFunction(),                                  3);

proof(testArray(),                                              4);
proof(testArray_concat(),                      "1,2,3,One,42,Two");
proof(testArray_join(),                               "0-1-2-3-4");
proof(testArray_length(),                                       7);
proof(testArray_reverse(),                            "4,3,2,1,0");
proof(testArray_slice(),                                  "4,3,5");
proof(testArray_sort(),                           "X,Z,d,m,r,v,y");
proof(testArray_pop_push(),                          "9 8 7 6 5 ");
proof(testArray_shift(),                              "10 11 12 ");

proof(testConstants_undefined(),                               42);

proof(testControlflow_break(),                                 43);
proof(testControlflow_breakLabeled(),                          42);
proof(testControlflow_continue(),                    "1234678910");
proof(testControlflow_do_while(),                              10);
proof(testControlflow_for(),                "0,1,2,3,4,5,6,7,8,9");
proof(testControlflow_for_in(), "a:Athens, b:Belgrade, c:Cairo, ");
proof(testControlflow_if_else(),                               20);
proof(testControlflow_switch(),               "Object is a Date.");

proof(zSq,                                                         25);
proof(derivedForm.blank(),                           "This is blank.");
proof(baseForm.blank(),                              "This is blank.");
proof(printFacts("A", [1, 4, 9], "B"),   "Name: AInfo: 20: 1,4,91: B");

proof(testDeclarations_this_Instance.color,                "Blue");
proof(testDeclarations_with(),                           "-26,37");

proof(testError_Object(),     "Error: No question, 42No question");

proof(testJIT_Function(),                                       5);
proof(testJIT_length(1, 2),                    "Args expected: 2");

proof(testGlobal_eval(),                                     true);
proof(testGlobal_parseInt(),                                   12);
proof(testGlobal_parseFloat(),                                1.2);

proof(testMath_all(),                                743008370728);

with (Number)
{
	print("# " + MAX_VALUE);
	print("# " + MIN_VALUE);
	proof(NEGATIVE_INFINITY, "-Infinity");
	proof(POSITIVE_INFINITY,  "Infinity");
}

proof(testObject_constructor(),             "Object is a String.");
proof(testObject_instanceof(),                               true);

proof(testOperators_delete(),                       "AthensCairo");
proof(testOperators_void(),                             undefined);
proof(testOperators_typeof(),                            "string");

proof(testRegExp_index(),     "0-3,The4-8,rain9-11,in12-17,Spain");
proof(testRegExp_source(),        "The string Spain contains in.");

proof(testString_charAt(),                                    "F");
proof(testString_charCodeAt(),                                 70);
proof(testString_fromCharCode(),                          "plain");
proof(testString_indexOf(),                                     4);
proof(testString_lastIndexOf(),                                 0);
proof(testString_split(),                    "The,rain,in,Spain.");
proof(testString_toLowerCase(),                  "i'm very loud!");
proof(testString_toUpperCase(),                 "I'M VERY QUIET.");
proof(testString_length(),                                     27);
proof(testString_slice(),                                 "hello");
proof(testString_match(),                           "Hello,World");
proof(testString_replace(),                  "rain The Spain. in");
proof(testString_search(),                                     18);

proof(John.printMailingLabel(),        "John Doe, Some place remote.");
proof(CPerson.printBlankLabel(),                            "-blank-");

proof(Jane.name + ", " + Jane.CreditLimit,            "Jane Doe, 500");
proof(Jane.printMailingLabel(),         "Jane Doe, Nice loft in NYC.");

proof(iftestC.displayName(),                    "This the form name.");
proof(iftestB.displayName(),                    "This the form name.");
proof(iftestA.displayName(),                    "This the form name.");

proof(chris.Age,                                                   27);
proof(chris.FavoriteColor,                                      "red");

proof(animalKangaroo.printQualities(),      "A kangaroo has a pouch.");
proof(animalDog.printQualities(),              "A dog has four legs.");

proof(hideTestDerv.methodA(),                       "Hiding methodA.");
proof(hideTestDerv.methodB(),                   "Overriding methodB.");
proof(hideTestBase.methodA(),                     "methodA of CBase.");
proof(hideTestBase.methodB(),                     "methodB of CBase.");

proof(constIndex,                                                   5);
proof(constName,                                   "Thomas Jefferson");
proof(constAnswer,                                                 42);
proof(constOneThird,                                            1./3.);
proof(constThings[1],                                        "thing1");

proof(testCar,                                                     42);

proof(testClass.x,                                                 10);
proof(testClass["x"],                                           "ten");
proof(testObject.x,                                          "twelve");
proof(testObject["x"],                                       "twelve");
proof(expstr,                          "Method called as a function.");
proof(exptest.x,                                                "123");
proof(expobj.x,                                                 "456");
proof(exptest.x,                                                "123");

proof(Greeting.Hello,                                    "Greetings!");
proof(France.Greeting.Hello,                               "Bonjour!");
proof(Deutschland.Greeting.Hello,                        "Guten tag!");
proof(France.Paris.Landmark.Tower,                     "Eiffel Tower");
proof(Units.distance,                                         "meter");
proof(France.Units.distance,                                  "meter");
proof(function(a,b){return a+b;},        "function(a,b){return a+b;}");

print("\n" + testEnumerator());



@set @myvar1 = (6 * 2)
@if (@myvar1 == 12)
	print("> PP works.");
@else
	print("> PP error.");
@end

unitTimer.Stop();

/* Report Results */
numberTests = passedTests + failedTests;
print("> Tests passed: " + passedTests + "/" + numberTests);
print("> Tests failed: " + failedTests + "/" + numberTests);
print("> Total time:   " + unitTimer.ElapsedMilliseconds + " ms");

function proof(a, b)
{
	numberTests += 1;

	if (a == b)
	{
		passedTests += 1;
	}
	else
	{
		failedTests += 1;
		print("# Failed (<" + a + "> != <" + b + ">)");
	}
}



























