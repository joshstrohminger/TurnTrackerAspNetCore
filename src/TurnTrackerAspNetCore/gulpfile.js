/// <binding BeforeBuild='default' Clean='clean' />

var gulp = require("gulp");
var rimraf = require("rimraf");
var merge = require('merge-stream');

var paths = {
    webroot: "./wwwroot/",
    node: "./node_modules/" 
};
paths.lib = paths.webroot + "lib/";
paths.libjs = paths.lib + 'js/';

gulp.task("clean", function(cb) {
    return rimraf(paths.lib, cb);
});

gulp.task('default', function () {
    var js = gulp.src([
            paths.node + 'jquery/dist/**/*',
            paths.node + 'jquery-validation/dist/jquery.validate.js',
            paths.node + 'jquery-validation-unobtrusive/jquery.validate.unobtrusive.js',
            
        ])
        .pipe(gulp.dest(paths.libjs));
    var bootstrap = gulp.src(paths.node + 'bootstrap/dist/**/*').pipe(gulp.dest(paths.lib));
    return merge(js, bootstrap);
});