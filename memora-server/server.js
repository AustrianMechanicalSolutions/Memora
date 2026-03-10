const express = require("express");
const path = require("path");

const app = express();

app.use(express.static(path.join(__dirname, "../memora-frontend/dist/memora-frontend")));

app.get("*", (req, res) => {
    res.sendFile(path.join(__dirname, "../memora-frontend/dist/memora-frontend/src/index.html"))
});

const PORT = process.env.PORT || 8080;
app.listen(PORT);