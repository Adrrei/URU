'use strict';

document.addEventListener('DOMContentLoaded', function () {
    const toggleMenu = document.getElementById('x-fade');

    toggleMenu.addEventListener('click', function () {
        const dropdown = document.getElementsByClassName('dropdown')[0];
        toggleMenu.classList.toggle('open');
        dropdown.classList.toggle('mobile-nav-show');
    });
});