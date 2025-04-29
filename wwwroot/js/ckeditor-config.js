document.addEventListener('DOMContentLoaded', function () {
    setTimeout(initializeCKEditor, 300); // Delay initialization slightly to ensure DOM readiness
});

// Main initialization function for CKEditor
function initializeCKEditor() {
    console.log('[DEBUG] Initializing CKEditor...');

    // Generate or retrieve a unique TempBlogId for managing uploads
    let tempBlogId = sessionStorage.getItem('tempBlogId');
    if (!tempBlogId) {
        tempBlogId = `temp_${Date.now()}_${Math.floor(Math.random() * 1000)}`;
        sessionStorage.setItem('tempBlogId', tempBlogId);
    }
    console.log(`[DEBUG] Using TempBlogId: ${tempBlogId}`);

    // Ensure TempBlogId is added as a hidden input field
    let tempInput = document.getElementById('TempBlogId');
    if (!tempInput) {
        tempInput = document.createElement('input');
        tempInput.type = 'hidden';
        tempInput.id = 'TempBlogId';
        tempInput.name = 'TempBlogId';
        document.querySelector('form').appendChild(tempInput);
    }
    tempInput.value = tempBlogId;

    console.log(`[DEBUG] TempBlogId set to: ${tempInput.value}`);

    // Destroy any existing CKEditor instances to prevent duplicates
    destroyExistingCKEditors();

    // CKEditor Configuration
    const editorSettings = {
        height: 500,
        extraPlugins: 'uploadimage,image,iframe,filebrowser', // Use 'image' instead of 'image2'
        removePlugins: '', // Remove 'image2'
        filebrowserUploadUrl: `/Admin/UploadFile?type=Images&blogId=${tempBlogId}`,
        filebrowserImageUploadUrl: `/Admin/UploadFile?type=Images&blogId=${tempBlogId}`,
        filebrowserUploadMethod: 'form',
        toolbar: [
            { name: 'document', items: ['Source'] },
            { name: 'clipboard', items: ['Cut', 'Copy', 'Paste', 'Undo', 'Redo'] },
            { name: 'editing', items: ['Find', 'Replace', 'SelectAll'] },
            { name: 'insert', items: ['Image', 'Table', 'HorizontalRule', 'SpecialChar', 'Embed'] },
            { name: 'basicstyles', items: ['Bold', 'Italic', 'Underline', 'Strike'] },
            { name: 'paragraph', items: ['NumberedList', 'BulletedList', 'Outdent', 'Indent'] },
            { name: 'styles', items: ['Format', 'Font', 'FontSize'] },
            { name: 'colors', items: ['TextColor', 'BGColor'] },
            { name: 'tools', items: ['Maximize'] }
        ]
    };
    

    // Initialize CKEditor for all language-specific content boxes
    initializeEditor('Content', editorSettings);
    initializeEditor('ContentUS', editorSettings);
    initializeEditor('ContentTR', editorSettings);
    initializeEditor('ContentDE', editorSettings);
    initializeEditor('ContentFR', editorSettings);
    initializeEditor('ContentAR', editorSettings);

    console.log('[DEBUG] CKEditor initialized successfully.');

    // Handle Cover Image Preview
    const coverImageInput = document.querySelector('input[name="ImageFile"]');
    if (coverImageInput) {
        coverImageInput.addEventListener('change', function () {
            const file = coverImageInput.files[0];
            if (file) {
                console.log(`[DEBUG] Cover image selected: ${file.name}`);
                const reader = new FileReader();
                reader.onload = function (e) {
                    const preview = document.getElementById('coverImagePreview');
                    if (preview) {
                        preview.src = e.target.result;
                    }
                };
                reader.readAsDataURL(file);
            }
        });
    }

    // Form validation to ensure TempBlogId is set before submission
    document.querySelector('form').addEventListener('submit', function (event) {
        if (!tempInput.value) {
            alert('Temporary Blog ID is missing!');
            event.preventDefault(); // Prevent form submission
        }
    });
}

// Function to initialize CKEditor instances for specific fields
function initializeEditor(id, config) {
    console.log(`→ trying to init editor on: ${id}`);
    const el = document.getElementById(id);
    if (!el) {
      console.warn(`❌ no element with id="${id}" found`);
      return;
    }
    if (CKEDITOR.instances[id]) {
      console.log(`⚠ already initialized: ${id}`);
      return;
    }
    console.log(`✅ attaching CKEditor to: ${id}`);
    CKEDITOR.replace(id, config);
  }

// Function to destroy any existing CKEditor instances
function destroyExistingCKEditors() {
    for (let instance in CKEDITOR.instances) {
        if (CKEDITOR.instances.hasOwnProperty(instance)) {
            CKEDITOR.instances[instance].destroy(true);
            console.log(`[DEBUG] Destroyed CKEditor instance: ${instance}`);
        }
    }
}
