const resizeObserver = new ResizeObserver(entries => {
    entries.forEach(entry => {
        const width = entry.contentRect.width;
        const desiredHeight = width / (16 / 9);
        console.log("Resizing", desiredHeight);
        entry.target.style.height = `${desiredHeight}px`;
    });
});

const carousel = document.querySelector("#myCarousel");
if (carousel) {
    resizeObserver.observe(carousel);
}
