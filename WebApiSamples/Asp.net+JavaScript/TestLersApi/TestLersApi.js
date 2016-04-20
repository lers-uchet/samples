/// <reference path="jquery-1.6.4.js" />

$(document).ready(function () {

	// Разрешаем кроссдоменное взаимодействие (cross-domain, XCC - Cross-site scripting) по технологии CORS (Cross-origin resource sharing)
	jQuery.support.cors = true;

	// Отправляем запрос на вход в систему
	SendLoginRequest('demo', 'demo');
});


function ReceivedLoginResponse(data) {
/// <summary>
/// Обработчик события успешного входа в систему
/// </summary>

	// Данные получаемые от сервера содержатся в свойстве d
	data = data.d;

	// Если есть ошибка, выводим её, иначе отправляем запрос на получение списка точек учета

	if (data.IsError)
		alert(data.ErrorMessage);
	else
		SendGetMeasurePointListRequest();
}

function ReceivedMeasurePointListResponse(data) {
/// <summary>
/// Обработчик события получения списка точек учета
/// </summary>

	// Данные получаемые от сервера содержатся в свойстве d
	data = data.d;

	// Если список точек учета не пустой отправляем запрос на получение потребления по первой точке учета из списка
	if (data.MeasurePointList.length > 0)
		SendGetMeasurePointConsumptionRequest(data.MeasurePointList[0].Id);
}

function ReceivedMeasurePointConsumptionResponse(data) {
/// <summary>
/// Обработчик события получение потребления по точке учета
/// </summary>

	// Данные получаемые от сервера содержатся в свойстве d
	data = data.d;

	// Если есть данные потребления, выводим сообщение со значением T_in за первую метку времени
	if (data.Data.length > 0)
	{
		var jsonDate = data.Data[0].DateTime

		var dataDate = new Date(parseInt(jsonDate.substr(6)));
		var T_in = data.Data[0].T_in;

		alert(dataDate.toString() + ': ' + T_in.toString());
	}
	else
		alert('По точке учета нет данных потребления.');

	// Получаем список объектов учета с расширенной информацией
	SendGetNodeListExtendedRequest();
}

function ReceivedNodeListExtendedResponse(data)
/// <summary>
/// Обработчик события получение списка объектов учета
/// </summary>
{
	// Данные получаемые от сервера содержатся в свойстве d
	data = data.d;
}

function SendLoginRequest(userName, password) {
/// <summary>
/// Отправляет запрос на вход в систему ЛЭРС УЧЕТ
/// </summary>

	$.ajax({
		type: "POST",
		url: "http://localhost:54025/Api.asmx/Login",
		contentType: "application/json; charset=utf-8",
		dataType: "json",
		xhrFields: { withCredentials: true },
		crossDomain: true,
		data: "{ 'userName':'" + userName + "','password':'" + password + "' }",
		success: ReceivedLoginResponse,
		error: function (jqXHR, textStatus, errorThrown) {
			console.error("jqXHR: %s \r\ntextStatus: %s \r\nerrorThrown:%s", jqXHR.responseText, textStatus, errorThrown);
			alert('Не удалось войти в систему.');
		}
	});
}

function SendGetMeasurePointListRequest() {
/// <summary>
/// Отправляет запрос на получение списка точек учета
/// </summary>

	$.ajax({
		type: "POST",
		url: "http://localhost:54025/Api.asmx/GetMeasurePointList",
		contentType: "application/json; charset=utf-8",
		dataType: "json",
		xhrFields: { withCredentials: true },
		crossDomain: true,
		data: "",
		success: ReceivedMeasurePointListResponse,
		error: function (jqXHR, textStatus, errorThrown) {
			alert('Не удалось получить список точек учета.');
		}
	});
}

function SendGetNodeListExtendedRequest() {
	/// <summary>
	/// Отправляет запрос на получение списка объектов учета
	/// </summary>

	$.ajax({
		type: "POST",
		url: "http://localhost:54025/Api.asmx/GetNodeListExtended",
		contentType: "application/json; charset=utf-8",
		dataType: "json",
		xhrFields: { withCredentials: true },
		crossDomain: true,
		data: "{ 'flags':" + 1024 + "}", // Запрашиваем помещения
		success: ReceivedNodeListExtendedResponse,
		error: function (jqXHR, textStatus, errorThrown) {
			alert('Не удалось получить список объектов учета.');
		}
	});
}

function SendGetMeasurePointConsumptionRequest(measurePointId) {
	/// <summary>
	/// Отправляет запрос на получение потребления за текущий месяц
	/// </summary>

	var nowDate = new Date();

	var currentStartDate = new Date(nowDate.getFullYear(), nowDate.getMonth(), 1);
	var currentEndDate = new Date(nowDate.getFullYear(), nowDate.getMonth(), nowDate.getDate());

	$.ajax({
		type: "POST",
		url: "http://localhost:54025/Api.asmx/GetMeasurePointConsumption",
		contentType: "application/json; charset=utf-8",
		dataType: "json",
		xhrFields: { withCredentials: true },
		crossDomain: true,
		data: "{ 'measurePointId':" + measurePointId + ", 'startDate':'" + currentStartDate.toJSON() + "','endDate':'" + currentEndDate.toJSON() + "', 'dataType': 'Day' }",
		success: ReceivedMeasurePointConsumptionResponse,
		error: function (jqXHR, textStatus, errorThrown) {
			alert('Не удалось получить данные потребления по точке учета.');
		}
	});
}

