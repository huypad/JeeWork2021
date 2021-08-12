import { environment } from 'src/environments/environment';

const tinyMCE = {

	plugins: 'paste print code preview  searchreplace autolink directionality  visualblocks visualchars fullscreen image link media  template codesample table charmap hr pagebreak nonbreaking anchor toc insertdatetime advlist lists wordcount imagetools textpattern help ',
	toolbar: 'undo redo | styleselect |  fontselect| bold italic underline | bullist numlist outdent indent | alignleft aligncenter alignright alignjustify | forecolor backcolor image link table  | removeformat code preview ',
	image_uploadtab: true,
	paste_as_text: true,
	height: 400,
	language: 'vi',
	// language_url : './assets/js/global/components/base/vi.js',
	language_url : './assets/tinymce/langs/vi.js',
	font_formats: 'Helvetica=Helvetica;UTM Avo=UTMAvo;',
	images_upload_url: environment.APIROOTS + '/Tool/upload-img?filename=' + 'GuideLine',
	automatic_uploads: true,
	images_upload_base_path: '/images',
	images_upload_credentials: true,
	//file_picker_callback: function (cb, value, meta) {
	//// Provide file and text for the link dialog

	//		if (meta.filetype == 'image') {
	//			var input = document.createElement('input');
	//			input.setAttribute('type', 'file');
	//			input.setAttribute('accept', 'image/*');

	//			// Note: In modern browsers input[type="file"] is functional without 
	//			// even adding it to the DOM, but that might not be the case in some older
	//			// or quirky browsers like IE, so you might want to add it to the DOM
	//			// just in case, and visually hide it. And do not forget do remove it
	//			// once you do not need it anymore.

	//			input.onchange = function () {
	//				var res = <HTMLInputElement>this;
	//				var file: File = res.files[0];
	//				var reader = new FileReader();
	//				const uploadData: FormData = new FormData();
	//				uploadData.append('file', file, file.name);

	//				reader.onload = function () {

	//				};
	//				reader.readAsDataURL(file);
	//			};

	//			input.click();
	//		}


	//},
	images_upload_handler: function (blobInfo, success, failure) {
		var xhr, formData;

		xhr = new XMLHttpRequest();
		xhr.withCredentials = false;
		xhr.open('POST', environment.APIROOTS + '/Tool/upload-img?filename=' + 'GuideLine');

		xhr.onload = function () {
			var json;

			if (xhr.status < 200 || xhr.status >= 300) {
				failure('HTTP Error: ' + xhr.status);
				return;
			}

			json = JSON.parse(xhr.responseText);
			if (!json || typeof json.imageUrl != 'string') {
				failure('Invalid JSON: ' + xhr.responseText);
				return;
			}

			success(json.imageUrl);
		};
		formData = new FormData();
		formData.append('file', blobInfo.blob(), blobInfo.filename());

		xhr.send(formData);
	},
	init_instance_callback: function () {
		var freeTiny = document.querySelector('.tox .tox-notification--in') as HTMLInputElement;
		if(freeTiny)
			freeTiny.style.display = 'none';
	},
	content_style: '.tox-notification--in{display:none};'
};
export { tinyMCE }
