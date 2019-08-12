document.addEventListener('DOMContentLoaded', function () {
    let toggleMenu = document.getElementById('x-fade');

    toggleMenu.addEventListener('click', function () {
        let dropdown = document.getElementsByClassName('dropdown')[0];
        toggleMenu.classList.toggle('open');
        dropdown.classList.toggle('mobile-nav-show');
    });
});