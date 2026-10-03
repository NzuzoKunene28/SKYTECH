function togglePw(id, icon) {
    var field = document.getElementById(id);
    if (field.type === 'password') {
        field.type = 'text';
    icon.classList.replace('fa-eye', 'fa-eye-slash');
    } else {
        field.type = 'password';
    icon.classList.replace('fa-eye-slash', 'fa-eye');
    }
}

    document.getElementById('pwField').addEventListener('input', function () {
    var val  = this.value;
    var bar  = document.getElementById('pwBar');
    var hint = document.getElementById('pwHint');
    var score = 0;
    if (val.length >= 8) score++;
    if (/[A-Z]/.test(val))   score++;
    if (/[0-9]/.test(val))   score++;
    if (/[^A-Za-z0-9]/.test(val)) score++;

    var pct    = ['0%','30%','55%','80%','100%'][score];
    var colour = ['#dc2626','#ea580c','#d97706','#16a34a','#0a7a3a'][score] || '#dc2626';
    var label  = ['Too short','Weak','Fair','Good','Strong'][score] || 'Too short';

    bar.style.width      = pct;
    bar.style.background = colour;
    hint.textContent     = val.length === 0 ? 'Enter a password' : label;
    hint.style.color     = colour;
});