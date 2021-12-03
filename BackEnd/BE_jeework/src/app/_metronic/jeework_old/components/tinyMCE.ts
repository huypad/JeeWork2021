import { AttachmentModel, FileUploadModel } from 'src/app/pages/JeeWork_Core/projects-team/Model/department-and-project.model';
import { environment } from 'src/environments/environment';

const tinyMCE = {
    // plugins: 'autoresize autosave paste print code preview searchreplace autolink directionality visualblocks visualchars fullscreen image link media  template codesample table charmap hr pagebreak nonbreaking anchor toc insertdatetime advlist lists wordcount imagetools textpattern help ',
    // plugins: 'imagetools codesample link image code paste table preview autolink fullscreen quickbars',
    plugins: ['imagetools', 'autolink', 'codesample', 'link', 'lists', 'media', 'table', 'image', 'fullscreen', 'quickbars'],
    toolbar: 'true | fullscreen | undo redo | bold italic underline | bullist numlist outdent indent | alignleft aligncenter alignright alignjustify | forecolor backcolor image link table | removeformat code paste pastetext preview',
    image_uploadtab: true,
    paste_block_drop: true,
    menubar: false,
    paste_as_text: true,
    paste_data_images: true,
    smart_paste: true,
    quickbars_selection_toolbar: ' forecolor | bold italic | h2 h3 | alignleft aligncenter alignright | bullist blockquote quicklink',
    content_style: 'img {max-width: 100%;}',
    imagetools_toolbar: 'alignleft aligncenter alignright | rotateleft rotateright | flipv fliph | editimage imageoptions | link',
    resize: true,
    height: 100,
    min_height: 100,
    max_height: 850,
    autoresize_bottom_margin: 25,
    autoresize_on_init: true,
    language: 'vi',
    language_url: './assets/tinymce/langs/vi.js',
    font_formats: 'Arial',
    table_default_styles: {
        'border-collapse': 'collapse',
        'width': '100%'
    },
    link_assume_external_targets: 'https',
    inline: false,
    table_responsive_width: true,
    images_upload_url: environment.APIROOTS + '/api/attachment/upload-img',
    default_link_target: '_blank',
    link_context_toolbar: true,
    theme_advanced_buttons3_add : "fullscreen",
    fullscreen_new_window : true,
    fullscreen_settings : {
      theme_advanced_path_location : "top",
        width : "640",
        height : "1000px"
    },
    templates: [
        { title: 'Test template 1', content: 'Test 1' },
        { title: 'Test template 2', content: 'Test 2' }
      ],
    content_css: [
        '//fonts.googleapis.com/css?family=Lato:300,300i,400,400i',
        '//www.tinymce.com/css/codepen.min.css'
      ],
    // automatic_uploads: true,
    // images_upload_base_path: '/images',
    // images_upload_credentials: true,
    // autosave_ask_before_unload: true, // Tự động save
    // autosave_interval: '10s', // thời gian tự động save
    // autosave_restore_when_empty: true,
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
        if (freeTiny) {
            freeTiny.style.display = 'none';
        }
    },
    setup: function (editor) {
        editor.on('BeforeSetContent', e => {
            if (e.content && e.content.includes('blob:')) {
                const s = e.content
                    .substr(e.content.indexOf('blob'), e.content.length)
                    .replace('/>', '')
                    .replace('>', '')
                    .replace('"', '')
                    .trim();
                if (e.target.editorUpload.blobCache.getByUri(s)) {
                    let size = e.target.editorUpload.blobCache.getByUri(s).blob().size;
                    const allowedSize = 2048; // KB
                    size = size / 1024; // KB
                    if (size > allowedSize) {
                        // alert('Hình ảnh đã vượt qua kích thước tối đa.');
                        editor.windowManager.alert('Hình ảnh đã vượt qua kích thước tối đa');
                        // editor.notificationManager.open({
                        // 	text: 'An error occurred.',
                        // 	type: 'error'
                        // });
                        e.preventDefault();
                        e.stopPropagation();
                    }
                }
            }
        });
    },
    function(ed){
        ed.on('init', function() { 
			(ed.getWin()).bind('resize', function(e){
				console.log('Editor window resized!');
			})
        });
    },
    
};
export { tinyMCE };
