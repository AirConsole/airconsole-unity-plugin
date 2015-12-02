function UnityProgress (dom) {
	this.progress = 0.0;
	this.message = "";
	this.dom = dom;

	var parent = dom.parentNode;
	
	/*
     * Unity WebGL Custom Progress Bar by Alexander Ocias
     * https://ocias.com/
     * https://ocias.com/blog/unity-webgl-custom-progress-bar/ 
     */

	createjs.CSSPlugin.install(createjs.Tween);
 	createjs.Ticker.setFPS(60);

	this.SetProgress = function (progress) { 
		if (this.progress < progress)
			this.progress = progress; 
		
		if (progress == 1) {
    	  this.SetMessage("Preparing...");
 			document.getElementById("spinner").style.display = "inherit";
			document.getElementById("bgBar").style.display = "none";
			document.getElementById("progressBar").style.display = "none";
    	} 
    	
		this.Update();
	}

	this.SetMessage = function (message) { 
		this.message = message; 
		this.Update();
	}

	this.Clear = function() {
		document.getElementById("loadingBox").style.display = "none";
	}

	this.Update = function() {
		var length = 200 * Math.min(this.progress, 1);
		bar = document.getElementById("progressBar")
		bar.style.width = length + "px";
		createjs.Tween.removeTweens(bar);
    	createjs.Tween.get(bar).to({width: length}, 500, createjs.Ease.sineOut);
		document.getElementById("loadingInfo").innerHTML = this.message;
	}

	this.Update ();
}