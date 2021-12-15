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
    fontsize_formats: "8pt 10pt 12pt 13pt 14pt 18pt 24pt 36pt",
    table_default_styles: {
        'border-collapse': 'collapse',
        'width': '100%'
    },
    mobile: {
        plugins: 'print preview  importcss searchreplace autolink autosave save directionality visualblocks visualchars fullscreen image link media template codesample table charmap hr pagebreak nonbreaking anchor toc insertdatetime advlist lists wordcount imagetools textpattern noneditable help charmap quickbars emoticons',
    },
    menu: {
        tc: {
            title: 'TinyComments',
            items: 'addcomment showcomments deleteallconversations'
        }
    },
    link_assume_external_targets: 'https',
    inline: false,
    table_responsive_width: true,
    images_upload_url: environment.APIROOTS + '/api/attachment/upload-img',
    default_link_target: '_blank',
    link_context_toolbar: true,
    theme_advanced_buttons3_add: "fullscreen",
    fullscreen_new_window: true,
    fullscreen_settings: {
        theme_advanced_path_location: "top",
        width: "640",
        height: "1000px"
    },
    link_list: [
        { title: 'My page 1', value: 'https://www.tiny.cloud' },
        { title: 'My page 2', value: 'http://www.moxiecode.com' }
    ],
    image_list: [
        { title: 'My page 1', value: 'https://www.tiny.cloud' },
        { title: 'My page 2', value: 'http://www.moxiecode.com' }
    ],
    image_class_list: [
        { title: 'None', value: '' },
        { title: 'Some class', value: 'class-name' }
    ],
    importcss_append: true,
    templates: [
        { title: 'New Table', description: 'creates a new table', content: '<div class="mceTmpl"><table width="98%%"  border="0" cellspacing="0" cellpadding="0"><tr><th scope="col"> </th><th scope="col"> </th></tr><tr><td> </td><td> </td></tr></table></div>' },
        { title: 'Starting my story', description: 'A cure for writers block', content: 'Once upon a time...' },
        { title: 'New list with dates', description: 'New List with dates', content: '<div class="mceTmpl"><span class="cdate">cdate</span><br /><span class="mdate">mdate</span><h2>My List</h2><ul><li></li><li></li></ul></div>' }
    ],
    template_cdate_format: '[Date Created (CDATE): %m/%d/%Y : %H:%M:%S]',
    template_mdate_format: '[Date Modified (MDATE): %m/%d/%Y : %H:%M:%S]',
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
    // tinymce.init({
    //     setup: (editor)=>{
    //        editor.on('init', ()=>{
    //           $(editor.contentDocument).find('a').prop('title', 'my new title');
    //        });
    //     }
    //  });
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
    function(ed) {
        ed.onInit.add(function (ed) {
            ed.on('init', function (e) {
            });
            // Get position before an execCommand is processed
            ed.on('BeforeExecCommand', function (e) {
                var cmd = e.command.toLowerCase();

                if (cmd !== 'undo' && cmd !== 'redo' && cmd !== 'mcerepaint') {
                    // endTyping();
                    // self.beforeChange();
                }
            });
            // Add undo level after an execCommand call was made
            ed.on('ExecCommand', function (e) {
                var cmd = e.command.toLowerCase();

                if (cmd !== 'undo' && cmd !== 'redo' && cmd !== 'mcerepaint') {
                    // addNonTypingUndoLevel(e);
                }
            });
        });
    },
};

export { tinyMCE };
