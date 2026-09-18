(function () {
    const sqlJsVersion = "1.10.3";
    const sqlJsBase = `https://cdnjs.cloudflare.com/ajax/libs/sql.js/${sqlJsVersion}/`;
    let sqlModulePromise;

    function loadScript(source) {
        return new Promise((resolve, reject) => {
            const existing = document.querySelector(`script[src="${source}"]`);
            if (existing) {
                resolve();
                return;
            }

            const script = document.createElement("script");
            script.src = source;
            script.async = true;
            script.onload = () => resolve();
            script.onerror = () => reject(new Error(`Could not load ${source}`));
            document.head.appendChild(script);
        });
    }

    async function getSqlModule() {
        if (!sqlModulePromise) {
            sqlModulePromise = (async () => {
                await loadScript(`${sqlJsBase}sql-wasm.js`);
                return window.initSqlJs({
                    locateFile: file => `${sqlJsBase}${file}`
                });
            })();
        }

        return sqlModulePromise;
    }

    function queryRows(database, sql, parameters) {
        const statement = database.prepare(sql);
        if (parameters) {
            statement.bind(parameters);
        }

        const rows = [];
        try {
            while (statement.step()) {
                rows.push(statement.getAsObject());
            }
        } finally {
            statement.free();
        }

        return rows;
    }

    function getInt(value) {
        return typeof value === "number" ? value : Number(value ?? 0);
    }

    window.anaaltomySqlite = {
        async loadDatabase(databaseBytes) {
            const SQL = await getSqlModule();
            const bytes = databaseBytes instanceof Uint8Array
                ? databaseBytes
                : new Uint8Array(databaseBytes);
            const database = new SQL.Database(bytes);

            try {
                const scans = queryRows(database, `
                    SELECT CommitScanId, InputPath, Status, StartedAtUtc, CompletedAtUtc
                    FROM CommitScan
                    ORDER BY CommitScanId DESC
                    LIMIT 1;
                `);

                if (scans.length === 0) {
                    throw new Error("The database does not contain any Anaaltomy scans.");
                }

                const scan = scans[0];
                const scanId = scan.CommitScanId;
                const counts = queryRows(database, `
                    SELECT
                        (SELECT COUNT(*) FROM ProjectScan WHERE CommitScanId = $scanId) AS ProjectCount,
                        (SELECT COUNT(*) FROM ScanFailure WHERE CommitScanId = $scanId) AS FailureCount;
                `, { $scanId: scanId })[0];

                const measurements = queryRows(database, `
                    SELECT Measurement.Dimension, Measurement.Bucket, SUM(Measurement.Count) AS Count
                    FROM ProjectScan
                    JOIN Measurement ON Measurement.ProjectScanId = ProjectScan.ProjectScanId
                    WHERE ProjectScan.CommitScanId = $scanId
                    GROUP BY Measurement.Dimension, Measurement.Bucket
                    ORDER BY Measurement.Dimension, Measurement.Bucket;
                `, { $scanId: scanId }).map(row => ({
                    Dimension: row.Dimension,
                    Bucket: row.Bucket,
                    Count: getInt(row.Count)
                }));

                const groupedMeasurements = queryRows(database, `
                    SELECT GroupedMeasurement.Dimension,
                           GroupedMeasurement.Bucket,
                           GroupedMeasurement.GroupDimension,
                           GroupedMeasurement.GroupBucket,
                           SUM(GroupedMeasurement.Count) AS Count
                    FROM ProjectScan
                    JOIN GroupedMeasurement ON GroupedMeasurement.ProjectScanId = ProjectScan.ProjectScanId
                    WHERE ProjectScan.CommitScanId = $scanId
                    GROUP BY GroupedMeasurement.Dimension,
                             GroupedMeasurement.Bucket,
                             GroupedMeasurement.GroupDimension,
                             GroupedMeasurement.GroupBucket
                    ORDER BY GroupedMeasurement.Dimension,
                             GroupedMeasurement.GroupDimension,
                             GroupedMeasurement.Bucket,
                             GroupedMeasurement.GroupBucket;
                `, { $scanId: scanId }).map(row => ({
                    Dimension: row.Dimension,
                    Bucket: row.Bucket,
                    GroupDimension: row.GroupDimension,
                    GroupBucket: row.GroupBucket,
                    Count: getInt(row.Count)
                }));

                const failures = queryRows(database, `
                    SELECT Stage, Message
                    FROM ScanFailure
                    WHERE CommitScanId = $scanId
                    ORDER BY ScanFailureId
                    LIMIT 20;
                `, { $scanId: scanId }).map(row => ({
                    Stage: row.Stage,
                    Message: row.Message
                }));

                return {
                    Scan: {
                        ScanId: getInt(scan.CommitScanId),
                        InputPath: scan.InputPath,
                        Status: scan.Status,
                        StartedAtUtc: scan.StartedAtUtc,
                        CompletedAtUtc: scan.CompletedAtUtc
                    },
                    ProjectCount: getInt(counts.ProjectCount),
                    FailureCount: getInt(counts.FailureCount),
                    Measurements: measurements,
                    GroupedMeasurements: groupedMeasurements,
                    Failures: failures
                };
            } finally {
                database.close();
            }
        }
    };
})();
