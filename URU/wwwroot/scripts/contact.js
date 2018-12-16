var numActiveFields = 0;
var fields = ['name', 'email', 'phone-number', 'subject', 'message'];

function alteredValue() {
    for (var i = 0; i < fields.length; i++) {
        var input = document.getElementById(fields[i]);
        if (input && input.value) {
            numActiveFields += 1;
        }
    }

    if (numActiveFields === 5) {
        document.getElementById('submit').classList.add('ready');
    } else {
        document.getElementById('submit').classList.remove('ready');
    }

    numActiveFields = 0;
}