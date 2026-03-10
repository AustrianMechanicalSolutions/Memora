const express = require("express");
const path = require("path");

const app = express();

const distPath = path.resolve(__dirname, "../memora-frontend/dist/memora-frontend/browser");

console.log("Serving Angular from:", distPath);

app.use(express.static(distPath));

app.get("/", (req, res) => {
  res.sendFile(path.join(distPath, "index.html"));
});

// Angular routing fallback
app.use((req, res) => {
  res.sendFile(path.join(distPath, "index.html"));
});

const PORT = process.env.PORT || 8080;

app.listen(PORT, () => {
  console.log("Server running on port " + PORT);
});