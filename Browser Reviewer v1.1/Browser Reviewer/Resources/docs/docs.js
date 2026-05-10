(function () {
  const state = {
    lang: localStorage.getItem("br-doc-lang") || "en",
    query: ""
  };

  function t(value) {
    if (!value) return "";
    return value[state.lang] || value.en || "";
  }

  function setLanguage(lang) {
    state.lang = lang;
    localStorage.setItem("br-doc-lang", lang);
    render();
  }

  function fieldMatches(field) {
    const q = state.query.trim().toLowerCase();
    if (!q) return true;
    return [
      field.name,
      field.type,
      t(field.concept),
      t(field.meaning),
      t(field.forensic),
      t(field.caution)
    ].join(" ").toLowerCase().includes(q);
  }

  function getSelectedDoc() {
    if (window.artifactDoc) return window.artifactDoc;
    if (!window.browserArtifactDocs) return null;
    const params = new URLSearchParams(window.location.search);
    const key = params.get("doc") || Object.keys(window.browserArtifactDocs)[0];
    return window.browserArtifactDocs[key] || null;
  }

  function renderArtifactPage() {
    const doc = getSelectedDoc();
    if (!doc) return;
    document.title = `${doc.title.en} | Browser Reviewer Docs`;

    document.querySelectorAll("[data-i18n]").forEach((node) => {
      const key = node.getAttribute("data-i18n");
      if (key === "title") node.textContent = t(doc.title);
      if (key === "summary") node.textContent = t(doc.summary);
      if (key === "source") node.textContent = t(doc.source);
      if (key === "potential") node.textContent = t(doc.potential);
      if (key === "example") node.textContent = t(doc.example);
      if (key === "browser") node.textContent = doc.browser || "";
      if (key === "family") node.textContent = doc.family || "";
      if (key === "confidence") node.textContent = doc.confidence || "";
      if (key === "investigation") node.textContent = t(doc.investigation);
      if (key === "caution") node.textContent = t(doc.caution);
    });

    document.querySelectorAll("[data-doc-icon]").forEach((node) => {
      if (doc.icon) node.setAttribute("src", doc.icon);
      node.setAttribute("alt", doc.browser || "Browser");
    });

    const fields = document.getElementById("fields");
    if (fields) {
      fields.innerHTML = "";
      doc.fields.filter(fieldMatches).forEach((field) => {
        const card = document.createElement("article");
        card.className = "field-card";
        card.innerHTML = `
          <div class="field-head">
            <div class="field-name">${field.name}</div>
            <div class="field-kind">${field.type || ""}</div>
          </div>
          ${field.concept ? `<p class="field-concept"><strong>${state.lang === "en" ? "What is it?" : "Que es?"}</strong> ${t(field.concept)}</p>` : ""}
          ${field.example ? `<p><strong>${state.lang === "en" ? "Example" : "Ejemplo"}:</strong> ${t(field.example)}</p>` : ""}
          <p><strong>${state.lang === "en" ? "In Browser Reviewer" : "En Browser Reviewer"}:</strong> ${t(field.meaning)}</p>
          <p><strong>${state.lang === "en" ? "Why it matters" : "Por que importa"}:</strong> ${t(field.forensic)}</p>
          ${field.caution ? `<p><strong>${state.lang === "en" ? "Caution" : "Precaucion"}:</strong> ${t(field.caution)}</p>` : ""}
        `;
        fields.appendChild(card);
      });
    }
  }

  function renderIndexPage() {
    const list = document.getElementById("artifactIndex");
    const source = window.browserArtifactIndex || window.artifactsIndex;
    if (!list || !source) return;

    const browserFilter = document.getElementById("browserFilter");
    const artifactFilter = document.getElementById("artifactFilter");
    const q = (document.getElementById("docSearch")?.value || "").trim().toLowerCase();
    const browserValue = browserFilter?.value || "";
    const artifactValue = artifactFilter?.value || "";

    list.innerHTML = "";
    source
      .filter((item) => !browserValue || item.browser === browserValue)
      .filter((item) => !artifactValue || item.artifact === artifactValue)
      .filter((item) => {
        if (!q) return true;
        return [item.browser, item.family, item.artifact, t(item.title), t(item.summary)].join(" ").toLowerCase().includes(q);
      })
      .forEach((item) => {
      const card = document.createElement("article");
      card.className = "artifact-card";
      card.innerHTML = `
        ${item.icon ? `<img class="browser-icon" src="${item.icon}" alt="${item.browser || ""}">` : ""}
        <h2><a href="${item.file}">${t(item.title)}</a></h2>
        <p>${t(item.summary)}</p>
        <div class="meta">
          ${item.family ? `<span class="pill">${item.family}</span>` : ""}
          <span class="pill">${item.fields} fields</span>
          <span class="pill ${item.confidence === "High" ? "good" : "warn"}">${item.confidence}</span>
        </div>
      `;
      list.appendChild(card);
    });
  }

  function populateFilters() {
    const browserFilter = document.getElementById("browserFilter");
    if (browserFilter && window.browserArtifactBrowsers) {
      window.browserArtifactBrowsers.forEach((browser) => {
        const option = document.createElement("option");
        option.value = browser;
        option.textContent = browser;
        browserFilter.appendChild(option);
      });
    }

    const artifactFilter = document.getElementById("artifactFilter");
    if (artifactFilter && window.browserArtifactArtifacts) {
      window.browserArtifactArtifacts.forEach((artifact) => {
        const option = document.createElement("option");
        option.value = artifact.key;
        option.textContent = artifact.title[state.lang] || artifact.title.en;
        artifactFilter.appendChild(option);
      });
    }
  }

  function render() {
    document.documentElement.lang = state.lang;
    document.querySelectorAll("[data-lang]").forEach((button) => {
      button.classList.toggle("active", button.getAttribute("data-lang") === state.lang);
    });
    document.querySelectorAll("[data-label-en]").forEach((node) => {
      node.textContent = state.lang === "en" ? node.getAttribute("data-label-en") : node.getAttribute("data-label-es");
    });
    const artifactFilter = document.getElementById("artifactFilter");
    if (artifactFilter && window.browserArtifactArtifacts) {
      Array.from(artifactFilter.options).forEach((option) => {
        if (!option.value) {
          const en = option.getAttribute("data-label-en") || "All artifacts";
          const es = option.getAttribute("data-label-es") || "Todos los artefactos";
          option.textContent = state.lang === "en" ? en : es;
          return;
        }
        const artifact = window.browserArtifactArtifacts.find((item) => item.key === option.value);
        if (artifact) option.textContent = artifact.title[state.lang] || artifact.title.en;
      });
    }
    renderArtifactPage();
    renderIndexPage();
  }

  document.addEventListener("DOMContentLoaded", () => {
    populateFilters();
    document.querySelectorAll("[data-lang]").forEach((button) => {
      button.addEventListener("click", () => setLanguage(button.getAttribute("data-lang")));
    });
    ["browserFilter", "artifactFilter", "docSearch"].forEach((id) => {
      const node = document.getElementById(id);
      if (node) {
        node.addEventListener("input", renderIndexPage);
        node.addEventListener("change", renderIndexPage);
      }
    });
    const search = document.getElementById("fieldSearch");
    if (search) {
      search.addEventListener("input", () => {
        state.query = search.value;
        renderArtifactPage();
      });
    }
    render();
  });
})();
