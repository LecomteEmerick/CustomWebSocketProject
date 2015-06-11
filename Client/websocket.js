var socket;
var debugLog;
var serverResponse;

function init()
{
	debugLog=document.getElementById("debugLog");
	serverResponse = document.getElementById("serverResponse");
}

function spam()
{
	socket.send("hello");
}

function connect()
{
	socket = new WebSocket("ws://127.0.0.1:80");
	//
	socket.onopen = function (event) {
		debugLog.innerHTML="Connected";
		//var timer=setInterval("spam()", 5);
	};
	socket.onmessage = function (event) {
		debugLog.innerHTML="Message";
		serverResponse.innerHTML = event.data;
	};
	socket.onerror = function (event) {
		debugLog.innerHTML="Error";
	};
	socket.onclose = function (event) {
		debugLog.innerHTML="Close";
	};
}

function send()
{
	var message = document.getElementById("message").value;
	if(message != "")
		socket.send(message);
}