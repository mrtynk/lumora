const express = require("express");
const router = express.Router();

const { getScoresByChildId } = require("../controllers/scoreController");

router.get("/:childId", getScoresByChildId);

module.exports = router;