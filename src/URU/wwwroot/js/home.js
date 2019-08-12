'use strict';

document.addEventListener('DOMContentLoaded', function () {
    let info = document.getElementById('info');
    let people = '.no';
    let love = '@';
    let spamming = 'uru';
    let bots = 'ar';

    info.title = bots + love + spamming + people;
    info.href = 'mailto:' + info.title;
});