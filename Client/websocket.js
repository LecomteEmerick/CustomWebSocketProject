var socket;
var debugLog;
var serverResponse;

function init()
{
	debugLog=document.getElementById("debugLog");
	serverResponse = document.getElementById("serverResponse");
}

function connect()
{
	socket = new WebSocket("ws://10.250.4.155:80");
	//
	socket.onopen = function (event) {
		debugLog.innerHTML="Connected";
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