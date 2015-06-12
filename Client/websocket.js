var socket;
var debugLog;
var serverResponse;
var canvas;
var context;

function init()
{
	debugLog=document.getElementById("debugLog");
	serverResponse = document.getElementById("serverResponse");
	canvas = document.getElementById("myCanvas");
	context = canvas.getContext("2d");
	canvas.ondragover = function(e) {
		e.preventDefault();
		return false;
	};
	canvas.ondrop = function(e) {
        e.preventDefault();
        var file = e.dataTransfer.files[0],
            reader = new FileReader();
        reader.onload = function(event) {
            var img = new Image(),
				imgStr = event.target.result;
            img.src = event.target.result;
            img.onload = function(event) {
                context.height = canvas.height = this.height;
                context.width = canvas.width = this.width;
                context.drawImage(this, 0, 0);
            };
        };
        reader.readAsDataURL(file);
        return false;
    };
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
		var image = new Image();
		
	    var reader = new FileReader();
	    reader.onload = function(e) {
		    image.src = e.target.result;
			canvas.width = image.width;
			canvas.height = image.height;
			context.drawImage(image, 0, 0);
	    };
	    reader.readAsDataURL(event.data);
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

function sendImage()
{
    var data = canvas.toDataURL("image/png", 0.7);
	var blob = dataURItoBlob(data);
	socket.send(blob);
}

function dataURItoBlob(dataURI) {
    // convert base64/URLEncoded data component to raw binary data held in a string
    var byteString;
    if (dataURI.split(',')[0].indexOf('base64') >= 0)
        byteString = atob(dataURI.split(',')[1]);
    else
        byteString = unescape(dataURI.split(',')[1]);

    // separate out the mime component
    var mimeString = dataURI.split(',')[0].split(':')[1].split(';')[0];

    // write the bytes of the string to a typed array
    var ia = new Uint8Array(byteString.length);
    for (var i = 0; i < byteString.length; i++) {
        ia[i] = byteString.charCodeAt(i);
    }

    return new Blob([ia], {type:mimeString});
}
