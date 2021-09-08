import { AttachmentModel, FileUploadModel } from 'src/app/pages/JeeWork_Core/projects-team/Model/department-and-project.model';
import { environment } from 'src/environments/environment';

const tinyMCE = {

	plugins: 'autosave paste print code preview  searchreplace autolink directionality  visualblocks visualchars fullscreen image link media  template codesample table charmap hr pagebreak nonbreaking anchor toc insertdatetime advlist lists wordcount imagetools textpattern help ',
	toolbar: 'undo redo | styleselect |  fontselect| bold italic underline | bullist numlist outdent indent | alignleft aligncenter alignright alignjustify | forecolor backcolor image link table  | removeformat code preview ',
	image_uploadtab: true,
	paste_as_text: true,
	paste_data_images: true,
	// paste_block_drop: true, // Dán hình ảnh đã screenshot
	// paste_filter_drop: true,
	// smart_paste: true,  // note: default value for smart_paste is true
	// image_file_types: 'jpg,svg,webp',
	height: 400,
	language: 'vi',
	// link_default_protocol: 'https', // Link mặc định
	// default_link_target: '_blank',
	// language_url : './assets/js/global/components/base/vi.js',
	language_url: './assets/tinymce/langs/vi.js',
	font_formats: 'Helvetica=Helvetica;UTM Avo=UTMAvo;',
	images_upload_url: environment.APIROOTS + '/api/attachment/upload-img',
	// automatic_uploads: true,
	// images_upload_base_path: '/images',
	// images_upload_credentials: true,
	// autosave_ask_before_unload: true, // Tự động save
	// autosave_interval: '10s', // thời gian tự động save

	images_upload_handler: function (blobInfo, success, failure) {
		var xhr, formData;
		xhr = new XMLHttpRequest();
		xhr.withCredentials = false;
		xhr.open('POST', environment.APIROOTS + '/api/attachment/upload-img');
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
		if (freeTiny)
			freeTiny.style.display = 'none';
	},
	content_style: '.tox-notification--in{display:none};'
};
export { tinyMCE }
