# [URU](http://uru.no/)

URU is my personal website, where I create and showcase small projects (for fun!).

The current version is developed with **C#**, **[ASP.NET Core (v2.2.0)](https://www.microsoft.com/net)**, **[Font Awesome](https://fontawesome.com/)**, **JavaScript**, and **HTML5 & CSS3**.

## Noteworthy Development Discussions

Have a look at the **[Offline Changeset](https://github.com/Adrrei/URU/wiki/Offline-Changeset)** for changes in this project before it was made public.

#### September 25th, 2018 - Complete Redesign

**Bulma** has been removed, and so has **jQuery**. Bulma was great to work with, and I can recommend it for prototyping. However, the hassle of overriding styles got to me, and I ended up tossing it and moving on to something more minimal.

The site is now taking advantage of **Flexbox** instead, a layout model.
Since Bulma was removed, the site's design had to change as well, and thus I opted to remove the *Projects* and *About Me* pages entirely, and instead move them to the *Index* page.

I also realized that with the few lines of **JavaScript** I had, it was not necessary to import jQuery, thus I rewrote that too.

#### August 27th, 2018 - Switched to Bulma from Semantic UI

Initially, I chose **Semantic UI** due to its concise syntax (as opposed to **Bootstrap 3.3.x**) and some of its unique components.
However, I encountered lots of scaling issues, and the framework was not as responsive as I initially thought. Instead of hacking my way through it, I decided (since I do this for fun after all!) to research other potential frameworks.

I thus discovered **Bulma**, a pure CSS framework (i.e. no JavaScript).
Rewriting the application was a simple task, and went faster than I expected. So far, so good!
