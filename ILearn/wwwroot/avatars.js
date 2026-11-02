const skins = ['#f5d3b5', '#dfb18b', '#bd8058', '#8e593d', '#623d2e', '#e8b49b'];
const outfits = [
  '#6ccbb4',
  '#ad8be0',
  '#f0b85c',
  '#da7d93',
  '#79b1e3',
  '#b5c777',
  '#ce9569',
  '#90a7c4',
];
const AVATARS = Array.from({ length: 48 }, (_, i) => ({
  id: `avatar-${String(i + 1).padStart(2, '0')}`,
  name: `قهرمان ${i + 1}`,
}));
window.ilearnAvatar = function(canvas, id = 'avatar-01') {
  const ctx = canvas.getContext('2d');
  if (!ctx) return;
  const i = Math.max(
    0,
    AVATARS.findIndex((a) => a.id === id),
  );
  canvas.width = 64;
  canvas.height = 64;
  ctx.imageSmoothingEnabled = false;
  ctx.clearRect(0, 0, 64, 64);
  ctx.scale(2, 2);
  const rect = (color, x, y, w, h) => {
    ctx.fillStyle = color;
    ctx.fillRect(x, y, w, h);
  };
  const hair = ['#382b35', '#d9b66b', '#6d3c2a', '#d8dce3', '#283b48', '#ac5374'][i % 6];
  const skin = skins[Math.floor(i / 8)],
    outfit = outfits[i % 8];
  rect(hair, 10, 3, 12, 11);
  if (i % 3 === 0) rect(hair, 8, 8, 16, 12);
  if (i % 3 === 1) {
    rect(hair, 7, 6, 4, 7);
    rect(hair, 22, 7, 4, 6);
  }
  rect(skin, 11, 7, 10, 10);
  rect('#182331', 12, 10, 2, 2);
  rect('#182331', 18, 10, 2, 2);
  rect('#a66060', 15, 14, 3, 1);
  rect(outfit, 10, 17, 12, 9);
  rect(skin, 7, 18, 3, 8);
  rect(skin, 22, 18, 3, 8);
  if (i % 4 === 0) {
    rect(outfit, 8, 23, 16, 5);
    rect('#e9c977', 12, 22, 8, 2);
  } else {
    rect('#354055', 11, 26, 4, 5);
    rect('#354055', 18, 26, 4, 5);
  }
  rect('#1c2434', 10, 30, 6, 2);
  rect('#1c2434', 17, 30, 6, 2);
  if (i % 8 === 2) {
    rect('#dcca82', 8, 4, 16, 3);
    rect('#dcca82', 11, 1, 10, 4);
  }
  if (i % 8 === 3) {
    rect('#cadbeb', 11, 10, 10, 1);
    rect('#cadbeb', 11, 9, 4, 4);
    rect('#cadbeb', 17, 9, 4, 4);
  }
  if (i % 8 === 4) {
    rect('#c9d6df', 5, 18, 2, 12);
    rect('#e9c977', 3, 20, 6, 2);
  }
  if (i % 8 === 5) {
    rect('#d9d0ed', 24, 15, 2, 16);
    rect('#a9def0', 22, 12, 6, 5);
  }
  if (i % 8 === 6) {
    rect('#ede1c8', 14, 18, 4, 7);
    rect('#bc6777', 14, 18, 4, 2);
  }
  if (i % 8 === 7) {
    rect('#516f88', 6, 19, 4, 8);
    rect('#b8d3da', 6, 20, 4, 2);
  }
}

