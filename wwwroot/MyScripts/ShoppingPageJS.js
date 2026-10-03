//tiny interactive script for bottom nav active state(demo)
  
        (function() {
   // simulate active state for bottom nav (just demo)
        const navLinks = document.querySelectorAll('.bottom-nav a');
      navLinks.forEach(link => {
            link.addEventListener('click', function (e) {
                navLinks.forEach(l => l.classList.remove('active'));
                this.classList.add('active');
                // prevent actual navigation for demo
                if (this.getAttribute('href') === '#') e.preventDefault();
            });
      });
    })();
  