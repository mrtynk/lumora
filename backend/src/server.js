const express = require("express");
const cors = require("cors");
require("dotenv").config();

const pool = require("./db/pool");
const eventRoutes = require("./routes/eventRoutes");
const scoreRoutes = require("./routes/scoreRoutes");

const app = express();


app.use(cors());
app.use(express.json());

app.use("/api/events", eventRoutes);
app.use("/api/scores", scoreRoutes);

app.get("/", (req, res) => {
  res.json({
    message: "Lumora backend çalışıyor.",
    project: "Mobil Oyun Aracılığıyla Çocuk Davranış Analitiği",
  });
});

app.get("/api/db-test", async (req, res) => {
  try {
    const result = await pool.query("SELECT NOW() AS current_time");

    res.json({
      message: "PostgreSQL bağlantısı başarılı.",
      databaseTime: result.rows[0].current_time,
    });
  } catch (error) {
    console.error("Veritabanı bağlantı hatası:", error);

    res.status(500).json({
      message: "PostgreSQL bağlantısı başarısız.",
      error: error.message,
    });
  }
});

const PORT = process.env.PORT || 5000;

app.listen(PORT, () => {
  console.log(`Lumora backend ${PORT} portunda çalışıyor.`);
});