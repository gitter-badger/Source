function testAnonymousFunction()
{
	var test = function(a, b)
	{
		return a + b;
	}

	return test(1, 2);
}

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

interface IForm
{
	function blank() : String;
}