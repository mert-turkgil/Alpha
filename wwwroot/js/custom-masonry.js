document.addEventListener("DOMContentLoaded", function () {
    const loadMoreBtn = document.getElementById("loadMoreBtn");
    const blogItems = document.querySelectorAll(".blog-item");
    let visibleCount = 3; // Start with 3 visible blogs

    if (loadMoreBtn) {
        loadMoreBtn.addEventListener("click", function () {
            let hiddenItems = Array.from(blogItems).slice(visibleCount, visibleCount + 3);
            hiddenItems.forEach(item => item.classList.remove("d-none")); // Show next 3 blogs
            visibleCount += 3;

            // Hide the button if no more blogs are hidden
            if (visibleCount >= blogItems.length) {
                loadMoreBtn.style.display = "none";
            }
        });
    }
});
