const ws = new WebSocket('ws://localhost:8089/command');

ws.onopen = () => {
  console.log('Connected to server');
};

ws.onerror = (error) => {
  console.error('WebSocket error:', error);
};

document.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
  checkbox.addEventListener('change', (e) => {
    ws.send(`${e.target.id}|${e.target.checked}`);
  });
});

document.querySelectorAll('input[type="number"]').forEach(input => {
  input.addEventListener('change', (e) => {
    const value = parseFloat(e.target.value);
    if (!isNaN(value)) {
      ws.send(`${e.target.id}|${value}`);
    }
  });
});

document.querySelectorAll('input[data-toggle]').forEach(checkbox => {
  checkbox.addEventListener('change', (e) => {
    const targetId = e.target.getAttribute('data-toggle');
    document.getElementById(targetId).style.display = e.target.checked ? 'block' : 'none';
  });
});