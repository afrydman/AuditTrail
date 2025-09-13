var options = {
	chart: {
		height: 300,
		type: "line",
		toolbar: {
			show: false,
		},
	},
	dataLabels: {
		enabled: false,
	},
	stroke: {
		curve: "smooth",
		width: 3,
	},
	series: [
		{
			name: "Sensor 1 - Sala de Maquinas",
			data: [10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10],
		},
		{
			name: "Sensor 2 - Sala de Maquinas",
			data: [2, 8, 2, 8, 2, 8, 51,2, 8, 20, 2, 8,],
		},
	],
	grid: {
		borderColor: "#dae1ea",
		strokeDashArray: 5,
		xaxis: {
			lines: {
				show: true,
			},
		},
		yaxis: {
			lines: {
				show: false,
			},
		},
		padding: {
			top: 0,
			right: 0,
			bottom: 10,
			left: 0,
		},
	},
	xaxis: {
		categories: [
			"Jan",
			"Feb",
			"Mar",
			"Apr",
			"May",
			"Jun",
			"Jul",
			"Aug",
			"Sep",
			"Oct",
			"Nov",
			"Dec",
		],
	},
	yaxis: {
		labels: {
			show: false,
		},
	},
	colors: ["#000", "#299bff", "#66b7ff", "#a3d4ff", "#ffffff", "#red"],
	markers: {
		size: 0,
		opacity: 0.3,
		colors: ["#000", "#299bff", "#66b7ff", "#a3d4ff", "#ffffff", "#red"],
		strokeColor: "#ffffff",
		strokeWidth: 2,
		hover: {
			size: 7,
		},
	},
};

var chart = new ApexCharts(document.querySelector("#lineGraph2"), options);

chart.render();
