function jumpPage(destination) {
    document.getElementById(destination).scrollIntoView({
        behavior: 'smooth'
    });
}