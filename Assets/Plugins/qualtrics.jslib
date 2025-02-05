mergeInto(LibraryManager.library, {

	TransferData: function (type, data, url) {
		window.parent.postMessage({type: Pointer_stringify(type), payload: Pointer_stringify(data)}, Pointer_stringify(url));
		},

});
