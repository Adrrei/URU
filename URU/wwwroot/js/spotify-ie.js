document.addEventListener('DOMContentLoaded', function () {
    if (navigator.appName === 'Microsoft Internet Explorer' || !!(navigator.userAgent.match(/Trident/) || navigator.userAgent.match(/rv:11/))) {
        let content = document.getElementsByClassName('banner')[0];
        content.innerHTML = '<div class="middle">Hi,<br><br>It is with great sorrow to announce that this page does not support Internet Explorer.<br>This  is yet another indicator that you should consider moving forward, and grab one of the modern browsers.<br><br>Best regards,<br>The empty web page.</div>';
    }
});