(() => {
  const developmentHosts = ["localhost", "127.0.0.1"];
  const environment = developmentHosts.includes(window.location.hostname)
    ? "development"
    : "production";

  const apiBaseUrls = Object.freeze({
    development: "https://localhost:7054",
    // Configure the API URL for each client deployment here.
    production: ""
  });

  window.PROJECTAW_API_CONFIG = Object.freeze({
    environment,
    apiBaseUrl: apiBaseUrls[environment].replace(/\/$/, ""),
    credentials: "include"
  });
})();
