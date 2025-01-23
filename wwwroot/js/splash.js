document.addEventListener("DOMContentLoaded", function () {
    const splashPlaceholder = document.getElementById("splash-placeholder");
    const MINIMUM_SPLASH_TIME = 2000; // 2 seconds

    if (!sessionStorage.getItem("hasVisited")) {
        sessionStorage.setItem("hasVisited", "true");

        splashPlaceholder.innerHTML = `
            <div id="splash-screen" class="splash-screen">
                <video autoplay muted loop id="splash-video">
                    <source src="/videos/SplashVid.mp4" type="video/mp4">
                    Your browser does not support the video tag.
                </video>
                <div class="overlay">
                    <div class="loading-container">
                        <div class="spinner"></div>
                    </div>
                </div>
            </div>
        `;

        const splash = document.getElementById("splash-screen");
        const startTime = Date.now();

        document.body.style.overflow = "hidden";

        window.addEventListener("load", () => {
            const elapsedTime = Date.now() - startTime;
            const remainingTime = Math.max(0, MINIMUM_SPLASH_TIME - elapsedTime);

            setTimeout(() => {
                splash.style.animation = "fadeOut 1s forwards";
                setTimeout(() => {
                    splash.remove();
                    document.body.style.overflow = "auto";
                }, 1000);
            }, remainingTime);
        });
    } else {
        splashPlaceholder.remove();
        document.body.style.overflow = "auto";
    }
});
