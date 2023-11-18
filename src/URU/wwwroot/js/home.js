'use strict';

document.addEventListener('DOMContentLoaded', function () {
    const info = document.getElementById('info');
    const people = '.no';
    const love = '@';
    const spamming = 'uru';
    const bots = 'ar';

    info.title = `${bots}${love}${spamming}${people}`;
    info.href = `mailto: ${info.title}`;
});