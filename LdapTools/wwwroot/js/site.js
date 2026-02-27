// Configurações do Notyf
const _notyf = new Notyf({
    duration: 10000,
    dismissible: true,
    position: {
        x: 'right',
        y: 'top'
    }
});

//Sidebar
const hamBurger = document.querySelector(".toggle-btn");

hamBurger.addEventListener("click", function () {
    document.querySelector("#sidebar").classList.toggle("expand");
});

// Funções para abertura das modais
function ajaxRequestModal(url, modal, content) {
    $.ajax({
        type: 'GET',
        url: url,
        success: function (result) {
            if (result.message) {
                _notyf.error(result.message);
            } else {
                $("#" + content).html(result);
                $('#' + modal).modal('show');
            }
        },
        error: function () {
            _notyf.error('Ocorreu um erro ao tentar processar a requisição.');
        }
    });
}

// Configurações das modais
function setupModal(btn, controller, action, attrId, modal, content) {
    $(btn).click(function () {
        if (attrId == '') {
            var url = `/${controller}/${action}/`;
        } else {
            var id = $(this).attr(attrId);
            var url = `/${controller}/${action}/${id}`;
        }

        ajaxRequestModal(url, modal, content);
    });
}

// Modais da view Account->Profile
setupModal('.btn-change-password', 'Account', 'ChangePassword', 'user-id', 'modalChangePassword', 'changePassword');

// Modais da view Users-Index
setupModal('.btn-users-delete', 'Users', 'Delete', 'user-id', 'modalUsersDelete', 'usersDelete');
setupModal('.btn-users-import', 'Users', 'ImportLdapUsers', 'user-id', 'modalUsersImport', 'usersImport');

//Prepara a modal para utilizar o select2
function initModalSelect(modalId) {
    $(modalId).on('show.bs.modal', function (event) {
        $('#multiple-select-field').select2({
            dropdownParent: $(modalId),
            theme: "bootstrap-5",
            width: $(this).data('width') ? $(this).data('width') : $(this).hasClass('w-100') ? '100%' : 'style',
            placeholder: $(this).data('placeholder')
        });
    });
}

initModalSelect('#modalUsersImport');

// Função para validar a imagem
function validateImage(event) {
    const file = event.target.files[0];
    const allowedTypes = ['image/jpeg', 'image/png', 'image/bmp', 'image/webp'];

    if (!allowedTypes.includes(file.type)) {
        alert('Por favor, selecione um arquivo de imagem válido (JPEG, PNG, BMP ou WebP).');
        event.target.value = '';
    } else {
        previewImage(event);
    }
}

// Função para exibir um preview da imagem escolhida
function previewImage(event) {
    var reader = new FileReader();
    reader.onload = function () {
        var output = document.getElementById('imagePreview');
        output.src = reader.result;
        output.style.display = 'block';
    };
    reader.readAsDataURL(event.target.files[0]);
}